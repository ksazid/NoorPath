using System.Data;
using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;

namespace NoorPath.Booking.Infrastructure;

public sealed class BookingCheckoutService(
    BookingDbContext bookings,
    TimeProvider timeProvider) : IBookingCheckoutService
{
    public Task<BookingCheckoutSnapshot?> GetAsync(
        Guid bookingId,
        string accountId,
        CancellationToken cancellationToken) =>
        bookings.Bookings.AsNoTracking()
            .Where(item => item.Id == bookingId && item.AccountId == accountId)
            .Select(item => new BookingCheckoutSnapshot(
                item.Id,
                item.Reference,
                item.AccountId,
                item.InventoryHoldId,
                item.Currency,
                item.DueNow,
                item.State,
                item.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<BookingState?> GetStateAsync(
        Guid bookingId,
        CancellationToken cancellationToken) =>
        bookings.Bookings.AsNoTracking()
            .Where(item => item.Id == bookingId)
            .Select(item => (BookingState?)item.State)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> TryTransitionAsync(
        Guid bookingId,
        BookingState next,
        string correlationId,
        string causationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await bookings.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var booking = await bookings.Bookings
            .FromSqlInterpolated(
                $"SELECT * FROM booking.bookings WHERE \"Id\" = {bookingId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (booking is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (booking.State == next)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        if (!BookingPolicy.CanTransition(booking.State, next))
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        var now = timeProvider.GetUtcNow();
        booking.State = next;
        booking.UpdatedAtUtc = now;
        bookings.OutboxMessages.Add(new BookingOutboxRecord
        {
            EventId = Guid.NewGuid(),
            EventType = next switch
            {
                BookingState.PaymentInProgress => "BookingAwaitingPayment",
                BookingState.PaymentSucceeded => "BookingPaymentSucceeded",
                BookingState.PaymentFailed => "BookingPaymentFailed",
                BookingState.PaymentCancelled => "BookingPaymentCancelled",
                _ => "BookingPaymentStateChanged"
            },
            EventVersion = 1,
            OccurredAtUtc = now,
            AggregateType = "Booking",
            AggregateId = booking.Id,
            AggregateVersion = 1,
            CorrelationId = correlationId,
            CausationId = causationId,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                bookingId = booking.Id,
                bookingReference = booking.Reference,
                state = next.ToString()
            }),
            State = "Pending",
            CreatedAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            bookings.ChangeTracker.Clear();
            return await bookings.Bookings.AsNoTracking()
                .AnyAsync(
                    item => item.Id == bookingId && item.State == next,
                    cancellationToken);
        }
    }
}
