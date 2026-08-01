namespace NoorPath.Booking;

public sealed record BookingCheckoutSnapshot(
    Guid BookingId,
    string BookingReference,
    string AccountId,
    Guid InventoryHoldId,
    string Currency,
    decimal DueNow,
    BookingState State,
    DateTimeOffset UpdatedAtUtc);

public interface IBookingCheckoutService
{
    Task<BookingCheckoutSnapshot?> GetAsync(
        Guid bookingId,
        string accountId,
        CancellationToken cancellationToken);

    Task<BookingState?> GetStateAsync(
        Guid bookingId,
        CancellationToken cancellationToken);

    Task<bool> TryTransitionAsync(
        Guid bookingId,
        BookingState next,
        string correlationId,
        string causationId,
        CancellationToken cancellationToken);
}
