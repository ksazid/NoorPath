namespace NoorPath.Booking;

public enum BookingState
{
    PendingPayment,
    PaymentInProgress,
    PaymentSucceeded,
    PaymentFailed,
    PaymentCancelled,
    PendingConfirmation,
    Confirming,
    Confirmed,
    ConfirmationException,
    Cancelled
}

public enum BookingOccupancy
{
    Double,
    Triple,
    Quad
}

public sealed record BookingFinancialSnapshot(
    string Currency,
    decimal UnitPrice,
    decimal Total,
    decimal DueNow,
    decimal Remaining,
    IReadOnlyList<BookingInstalment> Instalments);

public sealed record BookingInstalment(
    int Sequence,
    DateOnly DueDate,
    decimal Amount);

public static class BookingPolicy
{
    public static int RequiredTravellerCount(BookingOccupancy occupancy) => occupancy switch
    {
        BookingOccupancy.Double => 2,
        BookingOccupancy.Triple => 3,
        BookingOccupancy.Quad => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
    };

    public static void ValidateSnapshot(
        BookingFinancialSnapshot snapshot,
        int travellerCount)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Currency.Length != 3
            || !snapshot.Currency.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException(
                "Currency must be a three-letter uppercase code.",
                nameof(snapshot));
        }

        if (travellerCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(travellerCount));

        if (snapshot.UnitPrice < 0
            || snapshot.Total < 0
            || snapshot.DueNow < 0
            || snapshot.Remaining < 0)
        {
            throw new ArgumentException(
                "Booking financial amounts cannot be negative.",
                nameof(snapshot));
        }

        if (snapshot.Total != snapshot.UnitPrice * travellerCount)
        {
            throw new ArgumentException(
                "Booking total must equal unit price multiplied by traveller count.",
                nameof(snapshot));
        }

        if (snapshot.Total != snapshot.DueNow + snapshot.Remaining)
        {
            throw new ArgumentException(
                "Booking due-now and remaining amounts must equal the total.",
                nameof(snapshot));
        }

        var expectedSequence = 1;
        decimal scheduled = 0;
        foreach (var instalment in snapshot.Instalments)
        {
            if (instalment.Sequence != expectedSequence++)
            {
                throw new ArgumentException(
                    "Booking instalments must use a contiguous sequence starting at one.",
                    nameof(snapshot));
            }

            if (instalment.Amount <= 0)
            {
                throw new ArgumentException(
                    "Booking instalment amounts must be positive.",
                    nameof(snapshot));
            }

            scheduled += instalment.Amount;
        }

        if (scheduled != snapshot.Remaining)
        {
            throw new ArgumentException(
                "Booking instalments must equal the remaining balance.",
                nameof(snapshot));
        }
    }

    public static bool CanTransition(
        BookingState current,
        BookingState next) => (current, next) switch
        {
            (BookingState.PendingPayment, BookingState.PaymentInProgress) => true,
            (BookingState.PendingPayment, BookingState.PaymentFailed) => true,
            (BookingState.PendingPayment, BookingState.PaymentCancelled) => true,
            (BookingState.PaymentInProgress, BookingState.PaymentSucceeded) => true,
            (BookingState.PaymentInProgress, BookingState.PaymentFailed) => true,
            (BookingState.PaymentInProgress, BookingState.PaymentCancelled) => true,
            (BookingState.PaymentFailed, BookingState.PaymentInProgress) => true,
            (BookingState.PaymentSucceeded, BookingState.PendingConfirmation) => true,
            (BookingState.PendingConfirmation, BookingState.Confirming) => true,
            (BookingState.Confirming, BookingState.Confirmed) => true,
            (BookingState.Confirming, BookingState.ConfirmationException) => true,
            (BookingState.PendingConfirmation, BookingState.ConfirmationException) => true,
            (BookingState.ConfirmationException, BookingState.Confirming) => true,
            (BookingState.Confirmed, BookingState.Cancelled) => true,
            _ => current == next
        };
}
