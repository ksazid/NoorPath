using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Operators;

public static class ConfirmationEndpoints
{
    public static void MapConfirmations(this WebApplication app)
    {
        app.MapPost("/api/v1/operator/bookings/{bookingId:guid}/confirmation/retry", RetryAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> RetryAsync(
        Guid bookingId,
        RetryConfirmationRequest request,
        HttpContext http,
        BookingDbContext bookings,
        IOperatorAccess operatorAccess,
        ConfirmationService confirmation,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length is < 8 or > 240)
        {
            return Results.Problem(
                statusCode: 400,
                title: "A recovery reason is required",
                extensions: CheckoutIdempotency.ProblemExtensions(http, "recovery_reason_required"));
        }

        var access = await operatorAccess.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return Results.Forbid();

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == bookingId && item.OperatorId == access.OperatorId, cancellationToken);
        if (booking is null)
            return Results.NotFound();
        if (booking.State != BookingState.ConfirmationException || booking.SettledPaymentAttemptId is null)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Confirmation is not recoverable from this state",
                extensions: CheckoutIdempotency.ProblemExtensions(http, "confirmation_not_recoverable"));
        }

        var auditId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        bookings.OutboxMessages.Add(new BookingOutboxRecord
        {
            EventId = auditId,
            EventType = "ConfirmationRecoveryRequested",
            EventVersion = 1,
            OccurredAtUtc = now,
            AggregateType = "Booking",
            AggregateId = booking.Id,
            AggregateVersion = 1,
            CorrelationId = http.TraceIdentifier,
            CausationId = booking.SettledPaymentAttemptId.Value.ToString("D"),
            Payload = JsonSerializer.Serialize(new { bookingId = booking.Id, actorAccountId = principal.AccountId.Value, reason = request.Reason.Trim(), requestedAtUtc = now }),
            State = "Pending",
            CreatedAtUtc = now
        });
        await bookings.SaveChangesAsync(cancellationToken);

        var state = await confirmation.ProcessAsync(
            booking.Id,
            booking.SettledPaymentAttemptId.Value,
            http.TraceIdentifier,
            auditId.ToString("D"),
            cancellationToken);
        return Results.Ok(new { bookingId = booking.Id, state = state?.ToString() });
    }

    public sealed record RetryConfirmationRequest(string Reason);
}
