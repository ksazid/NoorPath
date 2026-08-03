using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Operators;
using NoorPath.Payments.Infrastructure;

public static class CancellationRefundEndpoints
{
    public sealed record CancellationRequestBody(string ReasonCategory);
    public sealed record OperatorDecisionBody(int ExpectedVersion, string Reason);

    public static void MapCancellationRefunds(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/bookings/{bookingId:guid}/cancellation",
                GetCustomerCancellationAsync)
            .RequireAuthorization();
        app.MapPost(
                "/api/v1/bookings/{bookingId:guid}/cancellation-requests",
                RequestCancellationAsync)
            .RequireAuthorization();

        var operatorGroup = app.MapGroup("/api/v1/operator/cancellations")
            .RequireAuthorization();
        operatorGroup.MapGet("", ListOperatorCasesAsync);
        operatorGroup.MapGet("/{cancellationId:guid}", GetOperatorCaseAsync);
        operatorGroup.MapPost("/{cancellationId:guid}/approve", ApproveAsync);
        operatorGroup.MapPost("/{cancellationId:guid}/reject", RejectAsync);
        operatorGroup.MapPost("/{cancellationId:guid}/recover", RecoverAsync);

        app.MapPost(
                "/api/v1/operator/refunds/{refundId:guid}/execute",
                ExecuteRefundAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetCustomerCancellationAsync(
        Guid bookingId,
        HttpContext http,
        CancellationRefundService service,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var projection = await service.GetCustomerProjectionAsync(
            bookingId,
            principal.AccountId.Value,
            cancellationToken);
        return projection is null ? Results.NotFound() : Results.Ok(projection);
    }

    private static async Task<IResult> RequestCancellationAsync(
        Guid bookingId,
        CancellationRequestBody request,
        HttpContext http,
        CancellationRefundService service,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        if (!CheckoutIdempotency.TryRead(http, out var idempotencyKey, out var error))
            return error!;
        if (!Enum.TryParse<CancellationReasonCategory>(
                request.ReasonCategory,
                ignoreCase: true,
                out var reasonCategory))
        {
            return Problem(
                http,
                StatusCodes.Status400BadRequest,
                "invalid_cancellation_reason",
                "Choose one of the supported cancellation reason categories.");
        }

        var result = await service.CreateRequestAsync(
            bookingId,
            principal.AccountId.Value,
            reasonCategory,
            idempotencyKey!,
            http.TraceIdentifier,
            cancellationToken);
        return ToResult(http, result);
    }

    private static async Task<IResult> ListOperatorCasesAsync(
        string? state,
        HttpContext http,
        IOperatorAccess operatorAccess,
        BookingDbContext bookings,
        PaymentsDbContext payments,
        CancellationToken cancellationToken)
    {
        var access = await GetOperatorAccessAsync(http, operatorAccess, cancellationToken);
        if (access is null || !CanManage(access))
            return Results.Forbid();

        var query = bookings.CancellationRequests.AsNoTracking()
            .Where(item => item.OperatorId == access.OperatorId);
        if (!string.IsNullOrWhiteSpace(state)
            && Enum.TryParse<BookingCancellationState>(state, true, out var requestedState))
        {
            query = query.Where(item => item.State == requestedState);
        }

        var requests = await query
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        var requestIds = requests.Select(item => item.Id).ToArray();
        var refunds = await payments.Refunds.AsNoTracking()
            .Where(item => requestIds.Contains(item.CancellationRequestId))
            .ToDictionaryAsync(item => item.CancellationRequestId, cancellationToken);
        var items = requests.Select(item =>
        {
            refunds.TryGetValue(item.Id, out var refund);
            return new
            {
                cancellationId = item.Id,
                item.BookingId,
                item.State,
                customerStatus = CustomerStatus(item.State, refund?.State),
                item.Currency,
                item.SettledAmount,
                item.PercentageFee,
                item.NonRefundableAmount,
                item.RefundableAmount,
                item.PolicyVersion,
                item.Version,
                item.RequestedAtUtc,
                item.UpdatedAtUtc,
                item.FailureCode,
                refund = refund is null ? null : RefundProjection(refund)
            };
        });
        return Results.Ok(new { items });
    }

    private static async Task<IResult> GetOperatorCaseAsync(
        Guid cancellationId,
        HttpContext http,
        IOperatorAccess operatorAccess,
        BookingDbContext bookings,
        PaymentsDbContext payments,
        CancellationToken cancellationToken)
    {
        var access = await GetOperatorAccessAsync(http, operatorAccess, cancellationToken);
        if (access is null || !CanManage(access))
            return Results.Forbid();

        var request = await bookings.CancellationRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == cancellationId
                    && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (request is null)
            return Results.NotFound();

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleAsync(item => item.Id == request.BookingId, cancellationToken);
        var refund = await payments.Refunds.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CancellationRequestId == request.Id,
                cancellationToken);
        var audit = await bookings.CancellationAudits.AsNoTracking()
            .Where(item => item.CancellationRequestId == request.Id)
            .OrderBy(item => item.OccurredAtUtc)
            .Select(item => new
            {
                item.Action,
                item.Reason,
                item.ActorAccountId,
                item.OccurredAtUtc
            })
            .ToArrayAsync(cancellationToken);

        return Results.Ok(new
        {
            booking = new
            {
                booking.Id,
                booking.Reference,
                state = booking.State.ToString(),
                booking.Currency,
                booking.Total,
                booking.CancelledAtUtc
            },
            cancellation = CancellationRefundService.ToRequestProjection(request, refund),
            calculation = new
            {
                request.PolicyVersion,
                request.PolicyTimeZoneId,
                request.DepartureAtUtc,
                request.DaysBeforeDeparture,
                request.WindowMinimumDaysBeforeDeparture,
                request.FeeBasisPoints,
                request.Currency,
                request.SettledAmount,
                request.PercentageFee,
                request.NonRefundableAmount,
                request.RefundableAmount,
                request.RefundProcessingBusinessDays
            },
            refund = refund is null ? null : RefundProjection(refund),
            allowedActions = AllowedActions(request, refund),
            audit
        });
    }

    private static async Task<IResult> ApproveAsync(
        Guid cancellationId,
        OperatorDecisionBody body,
        HttpContext http,
        IOperatorAccess operatorAccess,
        CancellationRefundService service,
        CancellationToken cancellationToken)
    {
        var access = await GetOperatorAccessAsync(http, operatorAccess, cancellationToken);
        if (access is null || !CanManage(access))
            return Results.Forbid();
        var principal = http.User.GetCurrentPrincipal()!;
        return ToResult(http, await service.ApproveAsync(
            cancellationId,
            access.OperatorId,
            principal.AccountId.Value,
            body.ExpectedVersion,
            body.Reason,
            http.TraceIdentifier,
            cancellationToken));
    }

    private static async Task<IResult> RejectAsync(
        Guid cancellationId,
        OperatorDecisionBody body,
        HttpContext http,
        IOperatorAccess operatorAccess,
        CancellationRefundService service,
        CancellationToken cancellationToken)
    {
        var access = await GetOperatorAccessAsync(http, operatorAccess, cancellationToken);
        if (access is null || !CanManage(access))
            return Results.Forbid();
        var principal = http.User.GetCurrentPrincipal()!;
        return ToResult(http, await service.RejectAsync(
            cancellationId,
            access.OperatorId,
            principal.AccountId.Value,
            body.ExpectedVersion,
            body.Reason,
            http.TraceIdentifier,
            cancellationToken));
    }

    private static async Task<IResult> RecoverAsync(
        Guid cancellationId,
        OperatorDecisionBody body,
        HttpContext http,
        IOperatorAccess operatorAccess,
        CancellationRefundService service,
        CancellationToken cancellationToken)
    {
        var access = await GetOperatorAccessAsync(http, operatorAccess, cancellationToken);
        if (access is null || !CanManage(access))
            return Results.Forbid();
        var principal = http.User.GetCurrentPrincipal()!;
        return ToResult(http, await service.RecoverAsync(
            cancellationId,
            access.OperatorId,
            principal.AccountId.Value,
            body.ExpectedVersion,
            body.Reason,
            http.TraceIdentifier,
            cancellationToken));
    }

    private static async Task<IResult> ExecuteRefundAsync(
        Guid refundId,
        OperatorDecisionBody body,
        HttpContext http,
        IOperatorAccess operatorAccess,
        CancellationRefundService service,
        CancellationToken cancellationToken)
    {
        var access = await GetOperatorAccessAsync(http, operatorAccess, cancellationToken);
        if (access is null || !CanManage(access))
            return Results.Forbid();
        var principal = http.User.GetCurrentPrincipal()!;
        try
        {
            var result = await service.ExecuteRefundAsync(
                refundId,
                access.OperatorId,
                principal.AccountId.Value,
                body.ExpectedVersion,
                body.Reason,
                http.TraceIdentifier,
                cancellationToken);
            if (result.Value is null)
                return ToResult(http, result);
            return Results.Json(
                RefundProjection(result.Value),
                statusCode: result.StatusCode);
        }
        catch (Exception exception) when (exception.GetType().Name == "RefundNotFoundException")
        {
            return Results.NotFound();
        }
    }

    private static object[] AllowedActions(
        BookingCancellationRequestRecord request,
        RefundRecord? refund)
    {
        var actions = new List<object>();
        if (request.State == BookingCancellationState.Requested)
        {
            actions.Add(new { code = "approve", label = "Approve cancellation" });
            actions.Add(new { code = "reject", label = "Reject cancellation" });
        }
        if (request.State == BookingCancellationState.Exception)
            actions.Add(new { code = "recover", label = "Retry governed cancellation" });
        if (refund?.State is NoorPath.Payments.RefundState.Authorized or NoorPath.Payments.RefundState.Failed)
            actions.Add(new { code = "execute_refund", label = "Execute authorized refund", refundId = refund.Id });
        return actions.ToArray();
    }

    private static object RefundProjection(RefundRecord refund) => new
    {
        refundId = refund.Id,
        refund.BookingId,
        refund.CancellationRequestId,
        refund.Currency,
        entitledAmount = refund.Amount,
        refund.RefundedAmount,
        state = refund.State.ToString(),
        refund.Provider,
        refund.ProviderRefundId,
        refund.FailureCode,
        refund.Version,
        refund.CreatedAtUtc,
        refund.UpdatedAtUtc,
        refund.SettledAtUtc
    };

    private static async Task<OperatorAccess?> GetOperatorAccessAsync(
        HttpContext http,
        IOperatorAccess operatorAccess,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        return principal is null
            ? null
            : await operatorAccess.FindActiveMembershipAsync(
                principal.AccountId,
                cancellationToken);
    }

    private static bool CanManage(OperatorAccess access) =>
        access.IsAllowed(OperatorPermissions.OperationalSupport)
        || access.IsAllowed(OperatorPermissions.AdminAccess);

    private static IResult ToResult<T>(
        HttpContext http,
        CancellationOperationResult<T> result)
    {
        if (result.IsSuccess)
            return Results.Json(result.Value, statusCode: result.StatusCode);
        return Problem(http, result.StatusCode, result.Code!, result.Message!);
    }

    private static IResult Problem(
        HttpContext http,
        int statusCode,
        string code,
        string message) =>
        Results.Problem(
            statusCode: statusCode,
            title: message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["correlationId"] = http.TraceIdentifier
            });

    private static string CustomerStatus(
        BookingCancellationState state,
        NoorPath.Payments.RefundState? refundState) =>
        state switch
        {
            BookingCancellationState.Requested => "UnderReview",
            BookingCancellationState.Approved or BookingCancellationState.Applying => "Approved",
            BookingCancellationState.Rejected => "Rejected",
            BookingCancellationState.Exception => "RecoveryRequired",
            BookingCancellationState.Applied => refundState switch
            {
                NoorPath.Payments.RefundState.Authorized or NoorPath.Payments.RefundState.Processing => "RefundPending",
                NoorPath.Payments.RefundState.PartiallyRefunded => "PartiallyRefunded",
                NoorPath.Payments.RefundState.Refunded => "Refunded",
                NoorPath.Payments.RefundState.Failed => "RecoveryRequired",
                _ => "Cancelled"
            },
            _ => state.ToString()
        };
}
