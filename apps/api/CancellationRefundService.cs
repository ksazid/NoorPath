using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;

public sealed record CancellationFeeProjection(
    string Code,
    string Label,
    decimal Amount);

public sealed record CancellationPolicyProjection(
    bool Available,
    bool CanRequest,
    string Code,
    string Message,
    string? Version,
    string? TimeZoneId,
    int? DaysBeforeDeparture,
    int? RefundProcessingBusinessDays,
    string Currency,
    decimal SettledAmount,
    decimal PercentageFee,
    decimal NonRefundableAmount,
    decimal RefundableAmount,
    bool RequiresOperatorApproval,
    IReadOnlyList<CancellationFeeProjection> FeeComponents);

public sealed record CancellationRequestProjection(
    Guid Id,
    string State,
    string CustomerStatus,
    string ReasonCategory,
    string PolicyVersion,
    int Version,
    string Currency,
    decimal SettledAmount,
    decimal PercentageFee,
    decimal NonRefundableAmount,
    decimal RefundableAmount,
    int RefundProcessingBusinessDays,
    string? DecisionReason,
    string? FailureCode,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DecidedAtUtc,
    DateTimeOffset? AppliedAtUtc,
    Guid? RefundId,
    string? RefundState,
    string? RefundFailureCode,
    DateTimeOffset? RefundedAtUtc);

public sealed record CustomerCancellationProjection(
    Guid BookingId,
    string BookingState,
    CancellationPolicyProjection Policy,
    CancellationRequestProjection? Request,
    IReadOnlyList<string> ReasonCategories);

public sealed record CancellationOperationResult<T>(
    int StatusCode,
    string? Code,
    string? Message,
    T? Value)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;

    public static CancellationOperationResult<T> Success(T value, int statusCode = 200) =>
        new(statusCode, null, null, value);

    public static CancellationOperationResult<T> Failure(
        int statusCode,
        string code,
        string message) =>
        new(statusCode, code, message, default);
}

public sealed class CancellationRefundService(
    BookingDbContext bookings,
    PaymentsDbContext payments,
    InventoryDbContext inventory,
    CatalogueDbContext catalogue,
    CancellationPolicyProvider policyProvider,
    IRefundProviderGateway refundProvider,
    TimeProvider timeProvider,
    ILogger<CancellationRefundService> log)
{
    private static readonly BookingCancellationState[] ActiveCancellationStates =
    [
        BookingCancellationState.Requested,
        BookingCancellationState.Approved,
        BookingCancellationState.Applying,
        BookingCancellationState.Exception
    ];

    public async Task<CustomerCancellationProjection?> GetCustomerProjectionAsync(
        Guid bookingId,
        string accountId,
        CancellationToken cancellationToken)
    {
        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.AccountId == accountId,
                cancellationToken);
        if (booking is null)
            return null;

        var request = await bookings.CancellationRequests.AsNoTracking()
            .Where(item => item.BookingId == bookingId && item.AccountId == accountId)
            .OrderByDescending(item => item.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is not null)
        {
            var refund = await payments.Refunds.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.CancellationRequestId == request.Id,
                    cancellationToken);
            return new CustomerCancellationProjection(
                booking.Id,
                booking.State.ToString(),
                PolicyFromSnapshot(request),
                ToRequestProjection(request, refund),
                ReasonCategories());
        }

        var evaluated = await EvaluateAsync(booking, cancellationToken);
        return new CustomerCancellationProjection(
            booking.Id,
            booking.State.ToString(),
            evaluated.Policy,
            null,
            ReasonCategories());
    }

    public async Task<CancellationOperationResult<CustomerCancellationProjection>> CreateRequestAsync(
        Guid bookingId,
        string accountId,
        CancellationReasonCategory reasonCategory,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.AccountId == accountId,
                cancellationToken);
        if (booking is null)
        {
            return CancellationOperationResult<CustomerCancellationProjection>.Failure(
                StatusCodes.Status404NotFound,
                "booking_not_found",
                "The booking could not be found.");
        }

        var idempotencyKeyHash = CheckoutIdempotency.Hash(idempotencyKey);
        var requestFingerprint = CheckoutIdempotency.Hash(
            $"{accountId}\n{bookingId:D}\n{reasonCategory}");
        var existingByKey = await bookings.CancellationRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.AccountId == accountId
                    && item.IdempotencyKeyHash == idempotencyKeyHash,
                cancellationToken);
        if (existingByKey is not null)
        {
            if (!string.Equals(
                    existingByKey.RequestFingerprint,
                    requestFingerprint,
                    StringComparison.Ordinal))
            {
                return CancellationOperationResult<CustomerCancellationProjection>.Failure(
                    StatusCodes.Status409Conflict,
                    "idempotency_conflict",
                    "Use a new idempotency key for a different cancellation request.");
            }

            return CancellationOperationResult<CustomerCancellationProjection>.Success(
                (await GetCustomerProjectionAsync(bookingId, accountId, cancellationToken))!,
                StatusCodes.Status200OK);
        }

        if (booking.State != BookingState.Confirmed)
        {
            return CancellationOperationResult<CustomerCancellationProjection>.Failure(
                StatusCodes.Status409Conflict,
                "booking_not_cancellable",
                "Only a confirmed booking can enter the cancellation review workflow.");
        }

        var active = await bookings.CancellationRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.BookingId == bookingId
                    && ActiveCancellationStates.Contains(item.State),
                cancellationToken);
        if (active is not null)
        {
            return CancellationOperationResult<CustomerCancellationProjection>.Failure(
                StatusCodes.Status409Conflict,
                "active_cancellation_exists",
                "A cancellation request is already active for this booking.");
        }

        var evaluated = await EvaluateAsync(booking, cancellationToken);
        if (!evaluated.Policy.Available || !evaluated.Policy.CanRequest || evaluated.Entitlement is null)
        {
            return CancellationOperationResult<CustomerCancellationProjection>.Failure(
                StatusCodes.Status409Conflict,
                evaluated.Policy.Code,
                evaluated.Policy.Message);
        }

        var now = timeProvider.GetUtcNow();
        var entitlement = evaluated.Entitlement;
        var request = new BookingCancellationRequestRecord
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            AccountId = booking.AccountId,
            OperatorId = booking.OperatorId,
            State = BookingCancellationState.Requested,
            ReasonCategory = reasonCategory,
            PolicyVersion = entitlement.PolicyVersion,
            PolicyTimeZoneId = entitlement.PolicyTimeZoneId,
            DepartureAtUtc = entitlement.DepartureAtUtc,
            DaysBeforeDeparture = entitlement.DaysBeforeDeparture,
            WindowMinimumDaysBeforeDeparture = entitlement.WindowMinimumDaysBeforeDeparture,
            FeeBasisPoints = entitlement.FeeBasisPoints,
            Currency = entitlement.Currency,
            SettledAmount = entitlement.SettledAmount,
            PercentageFee = entitlement.PercentageFee,
            NonRefundableAmount = entitlement.NonRefundableAmount,
            RefundableAmount = entitlement.RefundableAmount,
            RefundProcessingBusinessDays = entitlement.RefundProcessingBusinessDays,
            CalculationJson = JsonSerializer.Serialize(entitlement),
            IdempotencyKeyHash = idempotencyKeyHash,
            RequestFingerprint = requestFingerprint,
            Version = 1,
            RequestedAtUtc = now,
            UpdatedAtUtc = now
        };

        await using var transaction = await bookings.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        bookings.CancellationRequests.Add(request);
        AddCancellationAudit(
            request,
            accountId,
            "cancellation_requested",
            reasonCategory.ToString(),
            new
            {
                entitlement.PolicyVersion,
                entitlement.Currency,
                entitlement.SettledAmount,
                entitlement.PercentageFee,
                entitlement.NonRefundableAmount,
                entitlement.RefundableAmount
            },
            correlationId,
            now);
        AddBookingOutbox(
            booking,
            "BookingCancellationRequested",
            request.Id,
            correlationId,
            accountId,
            now);

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            bookings.ChangeTracker.Clear();
            var racedByKey = await bookings.CancellationRequests.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.AccountId == accountId
                        && item.IdempotencyKeyHash == idempotencyKeyHash,
                    cancellationToken);
            if (racedByKey is not null
                && string.Equals(
                    racedByKey.RequestFingerprint,
                    requestFingerprint,
                    StringComparison.Ordinal))
            {
                return CancellationOperationResult<CustomerCancellationProjection>.Success(
                    (await GetCustomerProjectionAsync(bookingId, accountId, cancellationToken))!,
                    StatusCodes.Status200OK);
            }

            return CancellationOperationResult<CustomerCancellationProjection>.Failure(
                StatusCodes.Status409Conflict,
                "active_cancellation_exists",
                "A cancellation request is already active for this booking.");
        }

        log.LogInformation(
            "Cancellation request outcome=created bookingId={BookingId} cancellationId={CancellationId} policyVersion={PolicyVersion} refundableAmount={RefundableAmount} currency={Currency} correlationId={CorrelationId}",
            booking.Id,
            request.Id,
            request.PolicyVersion,
            request.RefundableAmount,
            request.Currency,
            correlationId);
        return CancellationOperationResult<CustomerCancellationProjection>.Success(
            (await GetCustomerProjectionAsync(bookingId, accountId, cancellationToken))!,
            StatusCodes.Status201Created);
    }

    public async Task<CancellationOperationResult<CancellationRequestProjection>> RejectAsync(
        Guid cancellationId,
        string operatorId,
        string actorId,
        int expectedVersion,
        string reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!ValidDecisionReason(reason))
        {
            return CancellationOperationResult<CancellationRequestProjection>.Failure(
                StatusCodes.Status400BadRequest,
                "decision_reason_required",
                "A decision reason between 5 and 500 characters is required.");
        }

        await using var transaction = await bookings.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var request = await LockCancellationAsync(cancellationId, cancellationToken);
        if (request is null || request.OperatorId != operatorId)
            return CancellationOperationResult<CancellationRequestProjection>.Failure(StatusCodes.Status404NotFound, "cancellation_not_found", "The cancellation request could not be found.");
        if (request.Version != expectedVersion)
            return Stale<CancellationRequestProjection>();
        if (request.State == BookingCancellationState.Rejected)
            return CancellationOperationResult<CancellationRequestProjection>.Success(ToRequestProjection(request, null));
        if (request.State != BookingCancellationState.Requested)
            return InvalidState<CancellationRequestProjection>(request.State);

        var now = timeProvider.GetUtcNow();
        request.State = BookingCancellationState.Rejected;
        request.DecisionReason = reason.Trim();
        request.DecisionActorAccountId = actorId;
        request.DecidedAtUtc = now;
        request.UpdatedAtUtc = now;
        request.Version++;
        AddCancellationAudit(request, actorId, "cancellation_rejected", reason.Trim(), null, correlationId, now);
        var booking = await bookings.Bookings.SingleAsync(item => item.Id == request.BookingId, cancellationToken);
        AddBookingOutbox(booking, "BookingCancellationRejected", request.Id, correlationId, actorId, now);
        await bookings.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CancellationOperationResult<CancellationRequestProjection>.Success(ToRequestProjection(request, null));
    }

    public async Task<CancellationOperationResult<CancellationRequestProjection>> ApproveAsync(
        Guid cancellationId,
        string operatorId,
        string actorId,
        int expectedVersion,
        string reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!ValidDecisionReason(reason))
        {
            return CancellationOperationResult<CancellationRequestProjection>.Failure(
                StatusCodes.Status400BadRequest,
                "decision_reason_required",
                "A decision reason between 5 and 500 characters is required.");
        }

        await using (var transaction = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            var request = await LockCancellationAsync(cancellationId, cancellationToken);
            if (request is null || request.OperatorId != operatorId)
                return CancellationOperationResult<CancellationRequestProjection>.Failure(StatusCodes.Status404NotFound, "cancellation_not_found", "The cancellation request could not be found.");
            if (request.Version != expectedVersion)
                return Stale<CancellationRequestProjection>();
            if (request.State != BookingCancellationState.Requested)
                return InvalidState<CancellationRequestProjection>(request.State);

            var now = timeProvider.GetUtcNow();
            request.State = BookingCancellationState.Approved;
            request.DecisionReason = reason.Trim();
            request.DecisionActorAccountId = actorId;
            request.DecidedAtUtc = now;
            request.UpdatedAtUtc = now;
            request.Version++;
            AddCancellationAudit(request, actorId, "cancellation_approved", reason.Trim(), null, correlationId, now);
            var booking = await bookings.Bookings.SingleAsync(item => item.Id == request.BookingId, cancellationToken);
            AddBookingOutbox(booking, "BookingCancellationApproved", request.Id, correlationId, actorId, now);
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return await ApplyApprovedAsync(cancellationId, operatorId, actorId, correlationId, cancellationToken);
    }

    public async Task<CancellationOperationResult<CancellationRequestProjection>> RecoverAsync(
        Guid cancellationId,
        string operatorId,
        string actorId,
        int expectedVersion,
        string reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!ValidDecisionReason(reason))
        {
            return CancellationOperationResult<CancellationRequestProjection>.Failure(
                StatusCodes.Status400BadRequest,
                "decision_reason_required",
                "A recovery reason between 5 and 500 characters is required.");
        }

        await using (var transaction = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            var request = await LockCancellationAsync(cancellationId, cancellationToken);
            if (request is null || request.OperatorId != operatorId)
                return CancellationOperationResult<CancellationRequestProjection>.Failure(StatusCodes.Status404NotFound, "cancellation_not_found", "The cancellation request could not be found.");
            if (request.Version != expectedVersion)
                return Stale<CancellationRequestProjection>();
            if (request.State != BookingCancellationState.Exception)
                return InvalidState<CancellationRequestProjection>(request.State);

            var now = timeProvider.GetUtcNow();
            request.State = BookingCancellationState.Approved;
            request.DecisionReason = reason.Trim();
            request.DecisionActorAccountId = actorId;
            request.FailureCode = null;
            request.UpdatedAtUtc = now;
            request.Version++;
            AddCancellationAudit(request, actorId, "cancellation_recovery_requested", reason.Trim(), null, correlationId, now);
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return await ApplyApprovedAsync(cancellationId, operatorId, actorId, correlationId, cancellationToken);
    }

    public async Task<CancellationOperationResult<RefundRecord>> ExecuteRefundAsync(
        Guid refundId,
        string operatorId,
        string actorId,
        int expectedVersion,
        string reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!ValidDecisionReason(reason))
        {
            return CancellationOperationResult<RefundRecord>.Failure(
                StatusCodes.Status400BadRequest,
                "execution_reason_required",
                "An execution reason between 5 and 500 characters is required.");
        }

        RefundRecord refund;
        PaymentAttemptRecord? paymentAttempt;
        await using (var transaction = await payments.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            refund = await payments.Refunds
                .FromSqlInterpolated($"SELECT * FROM payments.refunds WHERE \"Id\" = {refundId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new RefundNotFoundException();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == refund.BookingId && item.OperatorId == operatorId, cancellationToken);
            if (booking is null)
                return CancellationOperationResult<RefundRecord>.Failure(StatusCodes.Status404NotFound, "refund_not_found", "The refund could not be found.");
            if (refund.State is RefundState.Refunded or RefundState.NotRequired)
                return CancellationOperationResult<RefundRecord>.Success(refund);
            if (refund.Version != expectedVersion)
                return Stale<RefundRecord>();
            if (refund.State is not RefundState.Authorized and not RefundState.Failed)
            {
                return CancellationOperationResult<RefundRecord>.Failure(
                    StatusCodes.Status409Conflict,
                    "refund_state_conflict",
                    "The refund cannot be executed from its current state.");
            }

            paymentAttempt = refund.PaymentAttemptId is null
                ? null
                : await payments.PaymentAttempts.AsNoTracking().SingleOrDefaultAsync(
                    item => item.Id == refund.PaymentAttemptId,
                    cancellationToken);
            if (refund.Amount > 0 && string.IsNullOrWhiteSpace(paymentAttempt?.ProviderPaymentId))
            {
                return CancellationOperationResult<RefundRecord>.Failure(
                    StatusCodes.Status409Conflict,
                    "provider_payment_unavailable",
                    "The settled provider payment reference is unavailable for refund execution.");
            }

            var now = timeProvider.GetUtcNow();
            refund.State = RefundState.Processing;
            refund.FailureCode = null;
            refund.UpdatedAtUtc = now;
            refund.Version++;
            AddRefundAudit(refund, actorId, "refund_execution_requested", reason.Trim(), null, correlationId, now);
            await payments.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var providerResult = await refundProvider.ExecuteAsync(
            new RefundProviderRequest(
                refund.Id,
                refund.BookingId,
                refund.PaymentAttemptId,
                paymentAttempt?.ProviderPaymentId ?? string.Empty,
                refund.Currency,
                refund.Amount,
                refund.Id.ToString("N"),
                correlationId),
            cancellationToken);

        await using var completion = await payments.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        refund = await payments.Refunds
            .FromSqlInterpolated($"SELECT * FROM payments.refunds WHERE \"Id\" = {refundId} FOR UPDATE")
            .SingleAsync(cancellationToken);
        var completedAt = timeProvider.GetUtcNow();
        var statusCode = StatusCodes.Status200OK;
        switch (providerResult.Outcome)
        {
            case RefundProviderOutcome.Succeeded:
                if (providerResult.RefundedAmount < 0 || providerResult.RefundedAmount > refund.Amount)
                    throw new InvalidOperationException("Refund provider returned an invalid amount.");
                refund.State = providerResult.RefundedAmount < refund.Amount
                    ? RefundState.PartiallyRefunded
                    : RefundState.Refunded;
                refund.ProviderRefundId = providerResult.ProviderRefundId;
                refund.RefundedAmount = providerResult.RefundedAmount;
                refund.SettledAtUtc = completedAt;
                refund.FailureCode = null;
                break;
            case RefundProviderOutcome.Pending:
                refund.State = RefundState.Processing;
                refund.ProviderRefundId ??= providerResult.ProviderRefundId;
                statusCode = StatusCodes.Status202Accepted;
                break;
            case RefundProviderOutcome.Failed:
                refund.State = RefundState.Failed;
                refund.FailureCode = providerResult.FailureCode ?? "refund_provider_failed";
                statusCode = StatusCodes.Status502BadGateway;
                break;
            case RefundProviderOutcome.Unavailable:
                refund.State = RefundState.Authorized;
                refund.FailureCode = providerResult.FailureCode ?? "refund_provider_unavailable";
                statusCode = StatusCodes.Status503ServiceUnavailable;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        refund.UpdatedAtUtc = completedAt;
        refund.Version++;
        AddRefundAudit(
            refund,
            actorId,
            "refund_execution_completed",
            reason.Trim(),
            new { outcome = providerResult.Outcome.ToString(), providerResult.RefundedAmount, providerResult.FailureCode },
            correlationId,
            completedAt);
        AddPaymentOutbox(refund, correlationId, actorId, completedAt);
        await payments.SaveChangesAsync(cancellationToken);
        await completion.CommitAsync(cancellationToken);
        return CancellationOperationResult<RefundRecord>.Success(refund, statusCode);
    }

    private async Task<CancellationOperationResult<CancellationRequestProjection>> ApplyApprovedAsync(
        Guid cancellationId,
        string operatorId,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        BookingCancellationRequestRecord request;
        BookingRecord booking;
        await using (var transaction = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            request = await LockCancellationAsync(cancellationId, cancellationToken)
                ?? throw new InvalidOperationException("Cancellation request disappeared during approval.");
            if (request.OperatorId != operatorId)
                return CancellationOperationResult<CancellationRequestProjection>.Failure(StatusCodes.Status404NotFound, "cancellation_not_found", "The cancellation request could not be found.");
            if (request.State == BookingCancellationState.Applied)
            {
                var completedRefund = await payments.Refunds.AsNoTracking().SingleOrDefaultAsync(item => item.CancellationRequestId == request.Id, cancellationToken);
                return CancellationOperationResult<CancellationRequestProjection>.Success(ToRequestProjection(request, completedRefund));
            }
            if (request.State is not BookingCancellationState.Approved and not BookingCancellationState.Exception)
                return InvalidState<CancellationRequestProjection>(request.State);

            booking = await bookings.Bookings
                .FromSqlInterpolated($"SELECT * FROM booking.bookings WHERE \"Id\" = {request.BookingId} FOR UPDATE")
                .SingleAsync(cancellationToken);
            if (booking.State == BookingState.Confirmed)
            {
                if (!BookingPolicy.CanTransition(booking.State, BookingState.Cancelled))
                    throw new InvalidOperationException("Booking cancellation transition is not allowed.");
                booking.State = BookingState.Cancelled;
                booking.CancellationRequestId = request.Id;
                booking.CancelledAtUtc = timeProvider.GetUtcNow();
                booking.UpdatedAtUtc = booking.CancelledAtUtc.Value;
            }
            else if (booking.State != BookingState.Cancelled || booking.CancellationRequestId != request.Id)
            {
                return CancellationOperationResult<CancellationRequestProjection>.Failure(
                    StatusCodes.Status409Conflict,
                    "booking_state_conflict",
                    "The booking changed and can no longer be cancelled from this request.");
            }

            var now = timeProvider.GetUtcNow();
            request.State = BookingCancellationState.Applying;
            request.FailureCode = null;
            request.UpdatedAtUtc = now;
            request.Version++;
            AddCancellationAudit(request, actorId, "booking_cancellation_applied", request.DecisionReason, null, correlationId, now);
            AddBookingOutbox(booking, "BookingCancelled", request.Id, correlationId, actorId, now);
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            await ReleaseInventoryAsync(booking, request, actorId, correlationId, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException)
        {
            log.LogError(exception, "Cancellation inventory release failed bookingId={BookingId} cancellationId={CancellationId} correlationId={CorrelationId}", booking.Id, request.Id, correlationId);
            return await MarkExceptionAsync(request.Id, "inventory_release_failed", actorId, correlationId, cancellationToken);
        }

        try
        {
            await AuthorizeRefundAsync(booking, request, actorId, correlationId, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException)
        {
            log.LogError(exception, "Cancellation refund authorization failed bookingId={BookingId} cancellationId={CancellationId} correlationId={CorrelationId}", booking.Id, request.Id, correlationId);
            return await MarkExceptionAsync(request.Id, "refund_authorization_failed", actorId, correlationId, cancellationToken);
        }

        await using var completion = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        request = await LockCancellationAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Cancellation request disappeared during completion.");
        var completedAt = timeProvider.GetUtcNow();
        request.State = BookingCancellationState.Applied;
        request.AppliedAtUtc = completedAt;
        request.UpdatedAtUtc = completedAt;
        request.FailureCode = null;
        request.Version++;
        AddCancellationAudit(request, actorId, "cancellation_completed", request.DecisionReason, null, correlationId, completedAt);
        await bookings.SaveChangesAsync(cancellationToken);
        await completion.CommitAsync(cancellationToken);
        var refund = await payments.Refunds.AsNoTracking().SingleOrDefaultAsync(item => item.CancellationRequestId == request.Id, cancellationToken);
        return CancellationOperationResult<CancellationRequestProjection>.Success(ToRequestProjection(request, refund));
    }

    private async Task ReleaseInventoryAsync(
        BookingRecord booking,
        BookingCancellationRequestRecord request,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await inventory.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var existing = await inventory.Releases.SingleOrDefaultAsync(
            item => item.CancellationRequestId == request.Id,
            cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var commitment = await inventory.Commitments.SingleOrDefaultAsync(
            item => item.BookingId == booking.Id,
            cancellationToken) ?? throw new InvalidOperationException("The booking inventory commitment is unavailable.");
        var hold = await inventory.Holds
            .FromSqlInterpolated($"SELECT * FROM inventory.inventory_holds WHERE \"Id\" = {commitment.HoldId} FOR UPDATE")
            .SingleAsync(cancellationToken);
        if (hold.State == InventoryHoldState.Committed)
        {
            hold.State = InventoryHoldState.Released;
            hold.TerminalAtUtc = timeProvider.GetUtcNow();
        }
        else if (hold.State != InventoryHoldState.Released)
        {
            throw new InvalidOperationException("The committed inventory cannot be released from its current state.");
        }

        inventory.Releases.Add(new InventoryReleaseRecord
        {
            Id = Guid.NewGuid(),
            CommitmentId = commitment.Id,
            HoldId = hold.Id,
            BookingId = booking.Id,
            CancellationRequestId = request.Id,
            AccountId = booking.AccountId,
            Quantity = commitment.Quantity,
            ActorAccountId = actorId,
            Reason = "approved_booking_cancellation",
            CorrelationId = correlationId,
            ReleasedAtUtc = timeProvider.GetUtcNow()
        });
        await inventory.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AuthorizeRefundAsync(
        BookingRecord booking,
        BookingCancellationRequestRecord request,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await payments.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var existing = await payments.Refunds.SingleOrDefaultAsync(
            item => item.CancellationRequestId == request.Id,
            cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var settledAttempts = await payments.PaymentAttempts
            .Where(item => item.BookingId == booking.Id
                && item.AccountId == booking.AccountId
                && item.State == PaymentAttemptState.Succeeded)
            .OrderByDescending(item => item.SettledAtUtc)
            .ToArrayAsync(cancellationToken);
        if (settledAttempts.Any(item => item.Currency != request.Currency)
            || settledAttempts.Sum(item => item.Amount) != request.SettledAmount)
        {
            throw new InvalidOperationException("Settled payment facts no longer reconcile with the cancellation snapshot.");
        }

        var primary = settledAttempts.FirstOrDefault();
        if (request.SettledAmount > 0 && primary is null)
            throw new InvalidOperationException("A settled payment attempt is required for the refund.");

        var now = timeProvider.GetUtcNow();
        var refund = new RefundRecord
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            CancellationRequestId = request.Id,
            PaymentAttemptId = primary?.Id,
            AccountId = booking.AccountId,
            Currency = request.Currency,
            Amount = request.RefundableAmount,
            RefundedAmount = 0m,
            State = request.RefundableAmount == 0m ? RefundState.NotRequired : RefundState.Authorized,
            Provider = primary?.Provider ?? "none",
            IdempotencyKeyHash = CheckoutIdempotency.Hash(request.Id.ToString("N")),
            CorrelationId = correlationId,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        payments.Refunds.Add(refund);
        AddRefundAudit(refund, actorId, "refund_authorized", request.DecisionReason, new { request.SettledAmount, request.RefundableAmount }, correlationId, now);
        AddPaymentOutbox(refund, correlationId, actorId, now);
        await payments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<CancellationOperationResult<CancellationRequestProjection>> MarkExceptionAsync(
        Guid cancellationId,
        string failureCode,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var request = await LockCancellationAsync(cancellationId, cancellationToken)
            ?? throw new InvalidOperationException("Cancellation request disappeared during exception handling.");
        var now = timeProvider.GetUtcNow();
        request.State = BookingCancellationState.Exception;
        request.FailureCode = failureCode;
        request.UpdatedAtUtc = now;
        request.Version++;
        AddCancellationAudit(request, actorId, "cancellation_exception", request.DecisionReason, new { failureCode }, correlationId, now);
        await bookings.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var refund = await payments.Refunds.AsNoTracking().SingleOrDefaultAsync(item => item.CancellationRequestId == request.Id, cancellationToken);
        return CancellationOperationResult<CancellationRequestProjection>.Success(
            ToRequestProjection(request, refund),
            StatusCodes.Status202Accepted);
    }

    private async Task<(CancellationPolicyProjection Policy, CancellationEntitlement? Entitlement)> EvaluateAsync(
        BookingRecord booking,
        CancellationToken cancellationToken)
    {
        if (booking.State != BookingState.Confirmed)
        {
            return (UnavailablePolicy(booking, "booking_not_cancellable", "Only a confirmed booking can be requested for cancellation."), null);
        }

        var departure = await catalogue.DepartureBatches.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == booking.DepartureId, cancellationToken);
        if (departure is null)
        {
            return (UnavailablePolicy(booking, "departure_unavailable", "Departure facts are still being prepared. Try again later."), null);
        }

        if (!policyProvider.TryGet(out var definition, out var code, out var message)
            || definition is null)
        {
            return (UnavailablePolicy(booking, code, message), null);
        }

        var settled = await payments.PaymentAttempts.AsNoTracking()
            .Where(item => item.BookingId == booking.Id
                && item.AccountId == booking.AccountId
                && item.State == PaymentAttemptState.Succeeded)
            .ToArrayAsync(cancellationToken);
        if (settled.Any(item => item.Currency != booking.Currency))
        {
            return (UnavailablePolicy(booking, "payment_currency_mismatch", "Payment facts require human review before cancellation."), null);
        }

        var settledAmount = settled.Sum(item => item.Amount);
        var evaluation = CancellationPolicy.Evaluate(
            definition,
            timeProvider.GetUtcNow(),
            departure.DepartureDate,
            booking.Currency,
            settledAmount);
        if (!evaluation.IsEligible || evaluation.Entitlement is null)
        {
            return (new CancellationPolicyProjection(
                true,
                false,
                evaluation.Code,
                evaluation.Message,
                definition.Version,
                definition.TimeZoneId,
                null,
                definition.RefundProcessingBusinessDays,
                booking.Currency,
                settledAmount,
                0m,
                0m,
                0m,
                true,
                []), null);
        }

        return (PolicyFromEntitlement(evaluation.Entitlement, evaluation.Code, evaluation.Message, true), evaluation.Entitlement);
    }

    private static CancellationPolicyProjection PolicyFromSnapshot(
        BookingCancellationRequestRecord request) =>
        new(
            true,
            false,
            "policy_snapshotted",
            "This is the immutable cancellation estimate retained when the request was submitted.",
            request.PolicyVersion,
            request.PolicyTimeZoneId,
            request.DaysBeforeDeparture,
            request.RefundProcessingBusinessDays,
            request.Currency,
            request.SettledAmount,
            request.PercentageFee,
            request.NonRefundableAmount,
            request.RefundableAmount,
            true,
            FeeComponents(request.PercentageFee, request.NonRefundableAmount, request.FeeBasisPoints));

    private static CancellationPolicyProjection PolicyFromEntitlement(
        CancellationEntitlement entitlement,
        string code,
        string message,
        bool canRequest) =>
        new(
            true,
            canRequest,
            code,
            message,
            entitlement.PolicyVersion,
            entitlement.PolicyTimeZoneId,
            entitlement.DaysBeforeDeparture,
            entitlement.RefundProcessingBusinessDays,
            entitlement.Currency,
            entitlement.SettledAmount,
            entitlement.PercentageFee,
            entitlement.NonRefundableAmount,
            entitlement.RefundableAmount,
            true,
            entitlement.FeeComponents
                .Select(item => new CancellationFeeProjection(item.Code, item.Label, item.Amount))
                .ToArray());

    private static CancellationPolicyProjection UnavailablePolicy(
        BookingRecord booking,
        string code,
        string message) =>
        new(false, false, code, message, null, null, null, null, booking.Currency, 0m, 0m, 0m, 0m, true, []);

    private static IReadOnlyList<CancellationFeeProjection> FeeComponents(
        decimal percentageFee,
        decimal nonRefundable,
        int basisPoints)
    {
        var result = new List<CancellationFeeProjection>();
        if (percentageFee > 0)
            result.Add(new("policy_percentage_fee", $"Policy fee ({basisPoints / 100m:0.##}%)", percentageFee));
        if (nonRefundable > 0)
            result.Add(new("non_refundable_component", "Configured non-refundable component", nonRefundable));
        return result;
    }

    public static CancellationRequestProjection ToRequestProjection(
        BookingCancellationRequestRecord request,
        RefundRecord? refund) =>
        new(
            request.Id,
            request.State.ToString(),
            CustomerStatus(request.State, refund?.State),
            request.ReasonCategory.ToString(),
            request.PolicyVersion,
            request.Version,
            request.Currency,
            request.SettledAmount,
            request.PercentageFee,
            request.NonRefundableAmount,
            request.RefundableAmount,
            request.RefundProcessingBusinessDays,
            request.DecisionReason,
            request.FailureCode,
            request.RequestedAtUtc,
            request.UpdatedAtUtc,
            request.DecidedAtUtc,
            request.AppliedAtUtc,
            refund?.Id,
            refund?.State.ToString(),
            refund?.FailureCode,
            refund?.SettledAtUtc);

    private static string CustomerStatus(
        BookingCancellationState state,
        RefundState? refundState) =>
        state switch
        {
            BookingCancellationState.Requested => "UnderReview",
            BookingCancellationState.Approved or BookingCancellationState.Applying => "Approved",
            BookingCancellationState.Rejected => "Rejected",
            BookingCancellationState.Exception => "RecoveryRequired",
            BookingCancellationState.Applied => refundState switch
            {
                RefundState.Authorized or RefundState.Processing => "RefundPending",
                RefundState.PartiallyRefunded => "PartiallyRefunded",
                RefundState.Refunded => "Refunded",
                RefundState.Failed => "RecoveryRequired",
                _ => "Cancelled"
            },
            _ => state.ToString()
        };

    private static IReadOnlyList<string> ReasonCategories() =>
        Enum.GetNames<CancellationReasonCategory>();

    private async Task<BookingCancellationRequestRecord?> LockCancellationAsync(
        Guid cancellationId,
        CancellationToken cancellationToken) =>
        await bookings.CancellationRequests
            .FromSqlInterpolated($"SELECT * FROM booking.cancellation_requests WHERE \"Id\" = {cancellationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private void AddCancellationAudit(
        BookingCancellationRequestRecord request,
        string actorId,
        string action,
        string? reason,
        object? detail,
        string correlationId,
        DateTimeOffset occurredAt)
    {
        bookings.CancellationAudits.Add(new BookingCancellationAuditRecord
        {
            Id = Guid.NewGuid(),
            CancellationRequestId = request.Id,
            BookingId = request.BookingId,
            AccountId = request.AccountId,
            ActorAccountId = actorId,
            Action = action,
            Reason = reason,
            DetailJson = detail is null ? "{}" : JsonSerializer.Serialize(detail),
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAt
        });
    }

    private void AddBookingOutbox(
        BookingRecord booking,
        string eventType,
        Guid cancellationId,
        string correlationId,
        string causationId,
        DateTimeOffset occurredAt)
    {
        bookings.OutboxMessages.Add(new BookingOutboxRecord
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventVersion = 1,
            OccurredAtUtc = occurredAt,
            AggregateType = "Booking",
            AggregateId = booking.Id,
            AggregateVersion = 1,
            CorrelationId = correlationId,
            CausationId = causationId,
            Payload = JsonSerializer.Serialize(new
            {
                bookingId = booking.Id,
                cancellationRequestId = cancellationId,
                state = booking.State.ToString(),
                booking.CancellationRequestId,
                booking.CancelledAtUtc
            }),
            State = "Pending",
            CreatedAtUtc = occurredAt
        });
    }

    private void AddRefundAudit(
        RefundRecord refund,
        string actorId,
        string action,
        string? reason,
        object? detail,
        string correlationId,
        DateTimeOffset occurredAt)
    {
        payments.RefundAudits.Add(new RefundAuditRecord
        {
            Id = Guid.NewGuid(),
            RefundId = refund.Id,
            BookingId = refund.BookingId,
            CancellationRequestId = refund.CancellationRequestId,
            AccountId = refund.AccountId,
            ActorAccountId = actorId,
            Action = action,
            Reason = reason,
            DetailJson = detail is null ? "{}" : JsonSerializer.Serialize(detail),
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAt
        });
    }

    private void AddPaymentOutbox(
        RefundRecord refund,
        string correlationId,
        string causationId,
        DateTimeOffset occurredAt)
    {
        payments.OutboxMessages.Add(new PaymentOutboxRecord
        {
            EventId = Guid.NewGuid(),
            EventType = refund.State switch
            {
                RefundState.Refunded => "RefundSettled",
                RefundState.PartiallyRefunded => "RefundPartiallySettled",
                RefundState.Failed => "RefundFailed",
                RefundState.NotRequired => "RefundNotRequired",
                _ => "RefundAuthorized"
            },
            EventVersion = 1,
            OccurredAtUtc = occurredAt,
            AggregateType = "Refund",
            AggregateId = refund.Id,
            AggregateVersion = refund.Version,
            CorrelationId = correlationId,
            CausationId = causationId,
            Payload = JsonSerializer.Serialize(new
            {
                refundId = refund.Id,
                refund.BookingId,
                refund.CancellationRequestId,
                refund.Currency,
                refund.Amount,
                refund.RefundedAmount,
                state = refund.State.ToString(),
                refund.FailureCode
            }),
            State = "Pending",
            CreatedAtUtc = occurredAt
        });
    }

    private static bool ValidDecisionReason(string reason) =>
        !string.IsNullOrWhiteSpace(reason)
        && reason.Trim().Length is >= 5 and <= 500;

    private static CancellationOperationResult<T> Stale<T>() =>
        CancellationOperationResult<T>.Failure(
            StatusCodes.Status409Conflict,
            "stale_cancellation_version",
            "The cancellation case changed. Refresh it before trying again.");

    private static CancellationOperationResult<T> InvalidState<T>(
        BookingCancellationState state) =>
        CancellationOperationResult<T>.Failure(
            StatusCodes.Status409Conflict,
            "cancellation_state_conflict",
            $"The cancellation cannot be changed from {state}.");

    private sealed class RefundNotFoundException : Exception;
}
