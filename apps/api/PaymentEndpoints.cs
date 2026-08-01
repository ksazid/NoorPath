using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorPath.Booking;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;

public static class PaymentEndpoints
{
    private const int MaximumWebhookBytes = 256 * 1024;

    public static void MapPayments(this WebApplication app)
    {
        app.MapPost("/api/v1/bookings/{bookingId:guid}/payments", InitiateAsync)
            .RequireAuthorization();
        app.MapGet("/api/v1/payments/{paymentAttemptId:guid}", GetAsync)
            .RequireAuthorization();
        app.MapPost(
                "/api/v1/payments/{paymentAttemptId:guid}/checkout-callback",
                RecordCheckoutCallbackAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/payments/webhooks/razorpay", ProcessWebhookAsync);
    }

    private static async Task<IResult> InitiateAsync(
        Guid bookingId,
        HttpContext http,
        IBookingCheckoutService bookingCheckout,
        InventoryDbContext inventory,
        PaymentsDbContext payments,
        IPaymentProviderGateway gateway,
        IOptions<RazorpayOptions> razorpay,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        if (!CheckoutIdempotency.TryRead(
                http,
                out var idempotencyKey,
                out var idempotencyError))
        {
            return idempotencyError!;
        }

        var accountId = principal.AccountId.Value;
        var booking = await bookingCheckout.GetAsync(
            bookingId,
            accountId,
            cancellationToken);
        if (booking is null)
            return Results.NotFound();

        if (booking.State == BookingState.PaymentSucceeded)
            return PaymentAlreadySettled(http);
        if (booking.State is not (
            BookingState.PendingPayment or BookingState.PaymentFailed))
        {
            return BookingNotPayable(http);
        }

        if (booking.DueNow <= 0)
            return BookingNotPayable(http);

        var now = timeProvider.GetUtcNow();
        var hold = await inventory.Holds.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == booking.InventoryHoldId
                && item.AccountId == accountId,
                cancellationToken);
        if (hold is null)
            return Results.NotFound();
        if (hold.State != InventoryHoldState.Active || hold.ExpiresAtUtc <= now)
            return HoldNotActive(http);

        var idempotencyKeyHash = CheckoutIdempotency.Hash(idempotencyKey!);
        var requestFingerprint = CheckoutIdempotency.Hash(
            $"{accountId}\n{booking.BookingId:D}\n{booking.Currency}\n{booking.DueNow:0.00}");

        PaymentAttemptRecord? attempt;
        var resumeProviderCreation = false;

        await using (var transaction = await payments.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken))
        {
            attempt = await payments.PaymentAttempts
                .SingleOrDefaultAsync(item =>
                    item.AccountId == accountId
                    && item.IdempotencyKeyHash == idempotencyKeyHash,
                    cancellationToken);

            if (attempt is not null)
            {
                if (!string.Equals(
                        attempt.RequestFingerprint,
                        requestFingerprint,
                        StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return IdempotencyConflict(http);
                }

                resumeProviderCreation = attempt.State == PaymentAttemptState.Created;
                await transaction.CommitAsync(cancellationToken);
                if (!resumeProviderCreation)
                {
                    return Results.Ok(ToResponse(attempt, razorpay.Value));
                }
            }
            else
            {
                var existingForBooking = await payments.PaymentAttempts.AsNoTracking()
                    .Where(item => item.BookingId == booking.BookingId)
                    .Where(item => item.State == PaymentAttemptState.Created
                        || item.State == PaymentAttemptState.ProviderPending
                        || item.State == PaymentAttemptState.RequiresAction
                        || item.State == PaymentAttemptState.Succeeded)
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existingForBooking is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExistingPayment(http, existingForBooking.Id);
                }

                var attemptId = Guid.NewGuid();
                attempt = new PaymentAttemptRecord
                {
                    Id = attemptId,
                    BookingId = booking.BookingId,
                    AccountId = accountId,
                    Currency = booking.Currency,
                    Amount = booking.DueNow,
                    State = PaymentAttemptState.Created,
                    Provider = gateway.ProviderName,
                    ProviderSessionId = $"pending_{attemptId:N}",
                    IdempotencyKeyHash = idempotencyKeyHash,
                    RequestFingerprint = requestFingerprint,
                    CorrelationId = http.TraceIdentifier,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CheckoutExpiresAtUtc = hold.ExpiresAtUtc
                };
                payments.PaymentAttempts.Add(attempt);

                try
                {
                    await payments.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    payments.ChangeTracker.Clear();

                    var racedByKey = await payments.PaymentAttempts.AsNoTracking()
                        .SingleOrDefaultAsync(item =>
                            item.AccountId == accountId
                            && item.IdempotencyKeyHash == idempotencyKeyHash,
                            cancellationToken);
                    if (racedByKey is not null)
                    {
                        if (!string.Equals(
                                racedByKey.RequestFingerprint,
                                requestFingerprint,
                                StringComparison.Ordinal))
                        {
                            return IdempotencyConflict(http);
                        }

                        if (racedByKey.State != PaymentAttemptState.Created)
                            return Results.Ok(ToResponse(racedByKey, razorpay.Value));

                        attempt = racedByKey;
                        resumeProviderCreation = true;
                    }
                    else
                    {
                        var racedForBooking = await payments.PaymentAttempts.AsNoTracking()
                            .Where(item => item.BookingId == booking.BookingId)
                            .OrderByDescending(item => item.CreatedAtUtc)
                            .FirstOrDefaultAsync(cancellationToken);
                        return racedForBooking is null
                            ? PaymentUnavailable(http)
                            : ExistingPayment(http, racedForBooking.Id);
                    }
                }
            }
        }

        if (attempt is null)
            return PaymentUnavailable(http);

        PaymentCheckoutSession session;
        try
        {
            session = await gateway.CreateCheckoutAsync(
                new PaymentCheckoutRequest(
                    attempt.Id,
                    booking.BookingId,
                    booking.BookingReference,
                    accountId,
                    booking.Currency,
                    booking.DueNow,
                    attempt.CheckoutExpiresAtUtc,
                    $"np_{attempt.Id:N}",
                    http.TraceIdentifier),
                cancellationToken);
        }
        catch (PaymentProviderUnavailableException exception)
        {
            await RecordInitiationFailureAsync(
                attempt.Id,
                payments,
                timeProvider,
                cancellationToken);
            await bookingCheckout.TryTransitionAsync(
                booking.BookingId,
                BookingState.PaymentFailed,
                http.TraceIdentifier,
                attempt.Id.ToString("D"),
                cancellationToken);
            log.LogWarning(
                "Payment initiation outcome={Outcome} paymentAttemptId={PaymentAttemptId} bookingId={BookingId} provider={Provider} reason={Reason} correlationId={CorrelationId}",
                "provider_unavailable",
                attempt.Id,
                booking.BookingId,
                gateway.ProviderName,
                exception.Message,
                http.TraceIdentifier);
            return ProviderUnavailable(http);
        }

        var persisted = await payments.PaymentAttempts
            .SingleAsync(item => item.Id == attempt.Id, cancellationToken);
        if (persisted.State == PaymentAttemptState.Created)
        {
            persisted.ProviderSessionId = session.ProviderSessionId;
            persisted.State = PaymentAttemptState.ProviderPending;
            persisted.UpdatedAtUtc = timeProvider.GetUtcNow();
            persisted.CheckoutExpiresAtUtc = session.ExpiresAtUtc;
            payments.OutboxMessages.Add(CreateOutbox(
                "PaymentInitiated",
                persisted,
                http.TraceIdentifier,
                booking.BookingId.ToString("D"),
                timeProvider.GetUtcNow(),
                new
                {
                    paymentAttemptId = persisted.Id,
                    persisted.BookingId,
                    persisted.Provider,
                    persisted.ProviderSessionId,
                    persisted.Currency,
                    persisted.Amount
                }));

            try
            {
                await payments.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                payments.ChangeTracker.Clear();
                persisted = await payments.PaymentAttempts.AsNoTracking()
                    .SingleAsync(item => item.Id == attempt.Id, cancellationToken);
                if (!string.Equals(
                        persisted.ProviderSessionId,
                        session.ProviderSessionId,
                        StringComparison.Ordinal))
                {
                    return PaymentUnavailable(http);
                }
            }
        }

        await bookingCheckout.TryTransitionAsync(
            booking.BookingId,
            BookingState.PaymentInProgress,
            http.TraceIdentifier,
            persisted.Id.ToString("D"),
            cancellationToken);

        log.LogInformation(
            "Payment initiation outcome={Outcome} paymentAttemptId={PaymentAttemptId} bookingId={BookingId} provider={Provider} durationMs={DurationMs} correlationId={CorrelationId}",
            "provider_pending",
            persisted.Id,
            booking.BookingId,
            persisted.Provider,
            (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds,
            http.TraceIdentifier);

        return Results.Created(
            $"/api/v1/payments/{persisted.Id:D}",
            ToResponse(persisted, razorpay.Value));
    }

    private static async Task<IResult> GetAsync(
        Guid paymentAttemptId,
        HttpContext http,
        PaymentsDbContext payments,
        IOptions<RazorpayOptions> razorpay,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        var attempt = await payments.PaymentAttempts.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == paymentAttemptId
                && item.AccountId == principal.AccountId.Value,
                cancellationToken);
        return attempt is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(attempt, razorpay.Value));
    }

    private static async Task<IResult> RecordCheckoutCallbackAsync(
        Guid paymentAttemptId,
        CheckoutCallbackRequest request,
        HttpContext http,
        PaymentsDbContext payments,
        IPaymentCheckoutCallbackVerifier verifier,
        IOptions<RazorpayOptions> razorpay,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        var attempt = await payments.PaymentAttempts
            .SingleOrDefaultAsync(item =>
                item.Id == paymentAttemptId
                && item.AccountId == principal.AccountId.Value,
                cancellationToken);
        if (attempt is null)
            return Results.NotFound();

        if (!string.Equals(
                attempt.ProviderSessionId,
                request.ProviderSessionId,
                StringComparison.Ordinal))
        {
            return ProviderIdentityMismatch(http);
        }

        bool verified;
        try
        {
            verified = verifier.Verify(new PaymentCheckoutCallback(
                request.ProviderSessionId,
                request.ProviderPaymentId,
                request.Signature));
        }
        catch (PaymentProviderUnavailableException)
        {
            return ProviderUnavailable(http);
        }

        if (!verified)
            return InvalidProviderSignature(http);

        if (attempt.ProviderPaymentId is not null
            && !string.Equals(
                attempt.ProviderPaymentId,
                request.ProviderPaymentId,
                StringComparison.Ordinal))
        {
            return ProviderIdentityMismatch(http);
        }

        attempt.ProviderPaymentId = request.ProviderPaymentId;
        attempt.UpdatedAtUtc = timeProvider.GetUtcNow();
        await payments.SaveChangesAsync(cancellationToken);

        return Results.Accepted(
            $"/api/v1/payments/{attempt.Id:D}",
            new
            {
                paymentAttemptId = attempt.Id,
                state = attempt.State.ToString(),
                settlementVerified = false,
                message = "Payment identity verified. NoorPath is waiting for the authenticated provider settlement event.",
                payment = ToResponse(attempt, razorpay.Value)
            });
    }

    private static async Task<IResult> ProcessWebhookAsync(
        HttpContext http,
        PaymentsDbContext payments,
        IBookingCheckoutService bookingCheckout,
        IPaymentProviderEventVerifier verifier,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        if (http.Request.ContentLength > MaximumWebhookBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        await using var buffer = new MemoryStream();
        await http.Request.Body.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > MaximumWebhookBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var payload = buffer.ToArray();
        var headers = http.Request.Headers.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        PaymentProviderEvent? providerEvent;
        try
        {
            providerEvent = await verifier.VerifyAsync(
                payload,
                headers,
                cancellationToken);
        }
        catch (PaymentProviderUnavailableException)
        {
            return ProviderUnavailable(http);
        }
        catch (CryptographicException)
        {
            return InvalidProviderSignature(http);
        }
        catch (JsonException)
        {
            return InvalidProviderPayload(http);
        }

        if (providerEvent is null)
            return Results.Ok(new { accepted = true, ignored = true });

        var attemptId = await payments.PaymentAttempts.AsNoTracking()
            .Where(item =>
                item.Provider == providerEvent.Provider
                && item.ProviderSessionId == providerEvent.ProviderSessionId)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (attemptId is null)
        {
            log.LogWarning(
                "Payment webhook outcome={Outcome} provider={Provider} providerEventId={ProviderEventId} providerSessionId={ProviderSessionId} correlationId={CorrelationId}",
                "attempt_not_found",
                providerEvent.Provider,
                providerEvent.ProviderEventId,
                providerEvent.ProviderSessionId,
                http.TraceIdentifier);
            return Results.Accepted();
        }

        ProviderEventOutcome outcome;
        PaymentAttemptRecord attempt;
        await using (var transaction = await payments.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken))
        {
            var duplicate = await payments.ProviderEvents.AsNoTracking()
                .AnyAsync(item =>
                    item.Provider == providerEvent.Provider
                    && item.ProviderEventId == providerEvent.ProviderEventId,
                    cancellationToken);
            if (duplicate)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { accepted = true, duplicate = true });
            }

            attempt = await payments.PaymentAttempts
                .FromSqlInterpolated(
                    $"SELECT * FROM payments.payment_attempts WHERE \"Id\" = {attemptId.Value} FOR UPDATE")
                .SingleAsync(cancellationToken);

            outcome = PaymentPolicy.EvaluateProviderEvent(
                attempt.State,
                providerEvent.RequestedState);
            if (providerEvent.ProviderPaymentId is not null
                && attempt.ProviderPaymentId is not null
                && !string.Equals(
                    attempt.ProviderPaymentId,
                    providerEvent.ProviderPaymentId,
                    StringComparison.Ordinal))
            {
                outcome = ProviderEventOutcome.Rejected;
            }

            var receivedAt = timeProvider.GetUtcNow();
            payments.ProviderEvents.Add(new PaymentProviderEventRecord
            {
                Id = Guid.NewGuid(),
                PaymentAttemptId = attempt.Id,
                Provider = providerEvent.Provider,
                ProviderEventId = providerEvent.ProviderEventId,
                EventType = providerEvent.EventType,
                PayloadHash = providerEvent.PayloadHash,
                SignatureKeyId = providerEvent.SignatureKeyId,
                Outcome = outcome,
                CorrelationId = http.TraceIdentifier,
                ReceivedAtUtc = receivedAt,
                ProcessedAtUtc = receivedAt
            });

            if (outcome == ProviderEventOutcome.Applied)
            {
                attempt.State = providerEvent.RequestedState;
                attempt.ProviderPaymentId ??= providerEvent.ProviderPaymentId;
                attempt.UpdatedAtUtc = receivedAt;
                attempt.FailureCode = providerEvent.RequestedState == PaymentAttemptState.Failed
                    ? "provider_payment_failed"
                    : null;
                if (providerEvent.RequestedState == PaymentAttemptState.Succeeded)
                    attempt.SettledAtUtc = providerEvent.OccurredAtUtc;

                var eventType = providerEvent.RequestedState switch
                {
                    PaymentAttemptState.Succeeded => "PaymentSettled",
                    PaymentAttemptState.Failed => "PaymentFailed",
                    PaymentAttemptState.Cancelled => "PaymentCancelled",
                    _ => "PaymentStateChanged"
                };
                payments.OutboxMessages.Add(CreateOutbox(
                    eventType,
                    attempt,
                    http.TraceIdentifier,
                    providerEvent.ProviderEventId,
                    receivedAt,
                    new
                    {
                        paymentAttemptId = attempt.Id,
                        attempt.BookingId,
                        attempt.Provider,
                        attempt.ProviderSessionId,
                        attempt.ProviderPaymentId,
                        attempt.Currency,
                        attempt.Amount,
                        state = attempt.State.ToString(),
                        providerEventId = providerEvent.ProviderEventId,
                        providerEvent.OccurredAtUtc
                    }));
            }

            try
            {
                await payments.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                payments.ChangeTracker.Clear();
                var racedDuplicate = await payments.ProviderEvents.AsNoTracking()
                    .AnyAsync(item =>
                        item.Provider == providerEvent.Provider
                        && item.ProviderEventId == providerEvent.ProviderEventId,
                        cancellationToken);
                return racedDuplicate
                    ? Results.Ok(new { accepted = true, duplicate = true })
                    : PaymentUnavailable(http);
            }
        }

        if (outcome == ProviderEventOutcome.Applied)
        {
            var bookingState = providerEvent.RequestedState switch
            {
                PaymentAttemptState.Succeeded => BookingState.PaymentSucceeded,
                PaymentAttemptState.Failed => BookingState.PaymentFailed,
                PaymentAttemptState.Cancelled => BookingState.PaymentCancelled,
                _ => BookingState.PaymentInProgress
            };
            var transitioned = await bookingCheckout.TryTransitionAsync(
                attempt.BookingId,
                bookingState,
                http.TraceIdentifier,
                providerEvent.ProviderEventId,
                cancellationToken);
            if (!transitioned)
            {
                log.LogWarning(
                    "Payment webhook booking projection outcome={Outcome} paymentAttemptId={PaymentAttemptId} bookingId={BookingId} requestedBookingState={RequestedBookingState} providerEventId={ProviderEventId} correlationId={CorrelationId}",
                    "booking_transition_pending_recovery",
                    attempt.Id,
                    attempt.BookingId,
                    bookingState,
                    providerEvent.ProviderEventId,
                    http.TraceIdentifier);
            }
        }

        log.LogInformation(
            "Payment webhook outcome={Outcome} paymentAttemptId={PaymentAttemptId} bookingId={BookingId} provider={Provider} providerEventId={ProviderEventId} requestedState={RequestedState} correlationId={CorrelationId}",
            outcome,
            attempt.Id,
            attempt.BookingId,
            providerEvent.Provider,
            providerEvent.ProviderEventId,
            providerEvent.RequestedState,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            accepted = true,
            outcome = outcome.ToString(),
            paymentAttemptId = attempt.Id,
            state = attempt.State.ToString()
        });
    }

    private static async Task RecordInitiationFailureAsync(
        Guid attemptId,
        PaymentsDbContext payments,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        payments.ChangeTracker.Clear();
        var attempt = await payments.PaymentAttempts
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken);
        if (attempt is null || attempt.State != PaymentAttemptState.Created)
            return;

        attempt.State = PaymentAttemptState.Failed;
        attempt.FailureCode = "provider_unavailable";
        attempt.UpdatedAtUtc = timeProvider.GetUtcNow();
        await payments.SaveChangesAsync(cancellationToken);
    }

    private static PaymentOutboxRecord CreateOutbox(
        string eventType,
        PaymentAttemptRecord attempt,
        string correlationId,
        string causationId,
        DateTimeOffset occurredAt,
        object payload) => new()
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventVersion = 1,
            OccurredAtUtc = occurredAt,
            AggregateType = "PaymentAttempt",
            AggregateId = attempt.Id,
            AggregateVersion = 1,
            CorrelationId = correlationId,
            CausationId = causationId,
            Payload = JsonSerializer.Serialize(payload),
            State = "Pending",
            CreatedAtUtc = occurredAt
        };

    private static object ToResponse(
        PaymentAttemptRecord attempt,
        RazorpayOptions razorpay)
    {
        object? checkout = null;
        if (attempt.State is PaymentAttemptState.ProviderPending
            or PaymentAttemptState.RequiresAction)
        {
            checkout = new
            {
                provider = attempt.Provider,
                providerSessionId = attempt.ProviderSessionId,
                publicKeyId = razorpay.KeyId,
                amountSubunits = decimal.ToInt64(attempt.Amount * 100m),
                attempt.Currency,
                checkoutScriptUri = razorpay.CheckoutScriptUri,
                expiresAtUtc = attempt.CheckoutExpiresAtUtc
            };
        }

        return new
        {
            paymentAttemptId = attempt.Id,
            attempt.BookingId,
            attempt.Currency,
            attempt.Amount,
            state = attempt.State.ToString(),
            attempt.Provider,
            attempt.ProviderPaymentId,
            attempt.FailureCode,
            checkout,
            attempt.CreatedAtUtc,
            attempt.UpdatedAtUtc,
            attempt.SettledAtUtc
        };
    }

    public sealed record CheckoutCallbackRequest(
        string ProviderSessionId,
        string ProviderPaymentId,
        string Signature);

    private static IResult NotAuthenticated(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Authentication required",
        detail: "Sign in before creating or viewing a payment.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "authentication_required"));

    private static IResult BookingNotPayable(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "This booking is not ready for payment",
        detail: "Review the current booking and payment state before trying again.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "booking_not_payable"));

    private static IResult PaymentAlreadySettled(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Payment is already settled",
        detail: "Continue to booking confirmation instead of creating another payment.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "payment_already_settled"));

    private static IResult HoldNotActive(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Availability is no longer secured",
        detail: "Secure availability again before starting payment.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "inventory_hold_not_active"));

    private static IResult IdempotencyConflict(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Idempotency key conflict",
        detail: "Use a new idempotency key for a different payment request.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "idempotency_conflict"));

    private static IResult ExistingPayment(HttpContext http, Guid paymentAttemptId) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "A payment attempt is already active",
            detail: "Continue with the existing payment instead of creating another one.",
            extensions: new Dictionary<string, object?>(
                CheckoutIdempotency.ProblemExtensions(
                    http,
                    "payment_attempt_exists"))
            {
                ["paymentAttemptId"] = paymentAttemptId
            });

    private static IResult ProviderIdentityMismatch(HttpContext http) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Payment provider identity mismatch",
            detail: "The payment response does not match this checkout attempt.",
            extensions: CheckoutIdempotency.ProblemExtensions(
                http,
                "provider_identity_mismatch"));

    private static IResult InvalidProviderSignature(HttpContext http) =>
        Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Payment provider signature is invalid",
            detail: "The provider response could not be authenticated.",
            extensions: CheckoutIdempotency.ProblemExtensions(
                http,
                "invalid_provider_signature"));

    private static IResult InvalidProviderPayload(HttpContext http) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Payment provider payload is invalid",
            detail: "The provider payload could not be processed.",
            extensions: CheckoutIdempotency.ProblemExtensions(
                http,
                "invalid_provider_payload"));

    private static IResult ProviderUnavailable(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Payment provider is temporarily unavailable",
        detail: "No payment was confirmed. Try again safely after a short delay.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "payment_provider_unavailable"));

    private static IResult PaymentUnavailable(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Payment could not be updated safely",
        detail: "Review the current payment state before trying again.",
        extensions: CheckoutIdempotency.ProblemExtensions(
            http,
            "payment_unavailable"));
}
