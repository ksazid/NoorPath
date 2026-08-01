using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;

public sealed class ConfirmationService(
    BookingDbContext bookings,
    InventoryDbContext inventory,
    PaymentsDbContext payments,
    TimeProvider timeProvider,
    ILogger<ConfirmationService> log)
{
    public async Task<BookingState?> ProcessAsync(
        Guid bookingId,
        Guid paymentAttemptId,
        string correlationId,
        string causationId,
        CancellationToken cancellationToken)
    {
        var payment = await payments.PaymentAttempts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == paymentAttemptId, cancellationToken);
        if (payment is null || payment.BookingId != bookingId || payment.State != PaymentAttemptState.Succeeded)
        {
            log.LogWarning(
                "Confirmation rejected outcome={Outcome} bookingId={BookingId} paymentAttemptId={PaymentAttemptId} correlationId={CorrelationId}",
                "invalid_payment_fact",
                bookingId,
                paymentAttemptId,
                correlationId);
            return null;
        }

        BookingRecord booking;
        await using (var transaction = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            booking = await bookings.Bookings
                .FromSqlInterpolated($"SELECT * FROM booking.bookings WHERE \"Id\" = {bookingId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Payment references an unknown booking.");

            if (booking.AccountId != payment.AccountId || booking.Currency != payment.Currency || booking.DueNow != payment.Amount)
            {
                await SetExceptionAsync(booking, "payment_booking_mismatch", paymentAttemptId, correlationId, causationId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return booking.State;
            }

            if (booking.State == BookingState.Confirmed)
            {
                await transaction.CommitAsync(cancellationToken);
                return booking.State;
            }

            if (booking.SettledPaymentAttemptId is not null && booking.SettledPaymentAttemptId != paymentAttemptId)
            {
                await SetExceptionAsync(booking, "foreign_payment_attempt", paymentAttemptId, correlationId, causationId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return booking.State;
            }

            booking.SettledPaymentAttemptId = paymentAttemptId;
            booking.State = BookingState.Confirming;
            booking.ConfirmationExceptionCode = null;
            booking.UpdatedAtUtc = timeProvider.GetUtcNow();
            AddOutbox(booking, "BookingConfirmationStarted", correlationId, causationId);
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        InventoryCommitmentRecord? commitment = null;
        await using (var transaction = await inventory.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            var hold = await inventory.Holds
                .FromSqlInterpolated($"SELECT * FROM inventory.inventory_holds WHERE \"Id\" = {booking.InventoryHoldId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            commitment = await inventory.Commitments
                .SingleOrDefaultAsync(item => item.BookingId == booking.Id, cancellationToken);

            if (
                commitment is null
                && hold is not null
                && hold.AccountId == booking.AccountId
                && hold.QuoteId == booking.QuoteId
                && hold.State == InventoryHoldState.Active
                && hold.ExpiresAtUtc > payment.SettledAtUtc)
            {
                commitment = new InventoryCommitmentRecord
                {
                    Id = Guid.NewGuid(),
                    HoldId = hold.Id,
                    BookingId = booking.Id,
                    PaymentAttemptId = paymentAttemptId,
                    InventoryPoolId = hold.InventoryPoolId,
                    AccountId = booking.AccountId,
                    Quantity = hold.Quantity,
                    CorrelationId = correlationId,
                    CreatedAtUtc = timeProvider.GetUtcNow()
                };
                inventory.Commitments.Add(commitment);
                hold.State = InventoryHoldState.Committed;
                hold.TerminalAtUtc = commitment.CreatedAtUtc;
                await inventory.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await using (var transaction = await bookings.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            booking = await bookings.Bookings
                .FromSqlInterpolated($"SELECT * FROM booking.bookings WHERE \"Id\" = {bookingId} FOR UPDATE")
                .SingleAsync(cancellationToken);
            if (booking.State == BookingState.Confirmed)
            {
                await transaction.CommitAsync(cancellationToken);
                return booking.State;
            }

            if (commitment is null || commitment.PaymentAttemptId != paymentAttemptId || commitment.HoldId != booking.InventoryHoldId)
            {
                await SetExceptionAsync(booking, "inventory_commitment_unavailable", paymentAttemptId, correlationId, causationId, cancellationToken);
            }
            else
            {
                var now = timeProvider.GetUtcNow();
                booking.State = BookingState.Confirmed;
                booking.InventoryCommitmentId = commitment.Id;
                booking.ConfirmedAtUtc ??= now;
                booking.UpdatedAtUtc = now;
                booking.ConfirmationExceptionCode = null;
                AddOutbox(booking, "BookingConfirmed", correlationId, commitment.Id.ToString("D"));
                await bookings.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        log.LogInformation(
            "Confirmation outcome={Outcome} bookingId={BookingId} paymentAttemptId={PaymentAttemptId} inventoryCommitmentId={InventoryCommitmentId} correlationId={CorrelationId}",
            booking.State,
            booking.Id,
            paymentAttemptId,
            commitment?.Id,
            correlationId);
        return booking.State;
    }

    private async Task SetExceptionAsync(
        BookingRecord booking,
        string code,
        Guid paymentAttemptId,
        string correlationId,
        string causationId,
        CancellationToken cancellationToken)
    {
        booking.State = BookingState.ConfirmationException;
        booking.SettledPaymentAttemptId ??= paymentAttemptId;
        booking.ConfirmationExceptionCode = code;
        booking.UpdatedAtUtc = timeProvider.GetUtcNow();
        AddOutbox(booking, "ConfirmationExceptionRaised", correlationId, causationId);
        await bookings.SaveChangesAsync(cancellationToken);
    }

    private void AddOutbox(BookingRecord booking, string eventType, string correlationId, string causationId)
    {
        var now = timeProvider.GetUtcNow();
        bookings.OutboxMessages.Add(new BookingOutboxRecord
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventVersion = 1,
            OccurredAtUtc = now,
            AggregateType = "Booking",
            AggregateId = booking.Id,
            AggregateVersion = 1,
            CorrelationId = correlationId,
            CausationId = causationId,
            Payload = JsonSerializer.Serialize(new
            {
                bookingId = booking.Id,
                paymentAttemptId = booking.SettledPaymentAttemptId,
                inventoryCommitmentId = booking.InventoryCommitmentId,
                state = booking.State.ToString(),
                exceptionCode = booking.ConfirmationExceptionCode
            }),
            State = "Pending",
            CreatedAtUtc = now
        });
    }
}
