using NoorPath.Booking;
using Xunit;

namespace NoorPath.Booking.Tests;

public sealed class BookingAmendmentPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 7);

    [Theory]
    [InlineData(BookingState.PendingPayment)]
    [InlineData(BookingState.PaymentInProgress)]
    [InlineData(BookingState.PaymentSucceeded)]
    [InlineData(BookingState.PendingConfirmation)]
    [InlineData(BookingState.Confirming)]
    [InlineData(BookingState.ConfirmationException)]
    [InlineData(BookingState.Cancelled)]
    public void Only_confirmed_bookings_are_amendable(BookingState state)
    {
        Assert.False(BookingAmendmentPolicy.CanAmend(state));
    }

    [Fact]
    public void Confirmed_booking_is_amendable()
    {
        Assert.True(BookingAmendmentPolicy.CanAmend(BookingState.Confirmed));
    }

    [Fact]
    public void Proposal_requires_occupancy_matched_traveller_count()
    {
        var proposal = Proposal(
            BookingOccupancy.Triple,
            Traveller("One", 1),
            Traveller("Two", 2));

        var exception = Assert.Throws<ArgumentException>(() =>
            BookingAmendmentPolicy.ValidateProposal(
                BookingState.Confirmed,
                proposal,
                Today));

        Assert.Contains("Traveller count", exception.Message);
    }

    [Fact]
    public void Proposal_rejects_duplicate_traveller_ids()
    {
        var id = Guid.NewGuid();
        var proposal = new BookingAmendmentProposal(
            BookingOccupancy.Double,
            [
                new(id, 1, "First Traveller", new DateOnly(1990, 1, 1)),
                new(id, 2, "Second Traveller", new DateOnly(1992, 2, 2))
            ],
            "Correct the booked party.");

        var exception = Assert.Throws<ArgumentException>(() =>
            BookingAmendmentPolicy.ValidateProposal(
                BookingState.Confirmed,
                proposal,
                Today));

        Assert.Contains("unique", exception.Message);
    }

    [Fact]
    public void Proposal_rejects_future_or_today_date_of_birth()
    {
        var proposal = new BookingAmendmentProposal(
            BookingOccupancy.Double,
            [
                new(Guid.NewGuid(), 1, "First Traveller", Today),
                Traveller("Second Traveller", 2)
            ],
            "Correct the booked party.");

        var exception = Assert.Throws<ArgumentException>(() =>
            BookingAmendmentPolicy.ValidateProposal(
                BookingState.Confirmed,
                proposal,
                Today));

        Assert.Contains("past", exception.Message);
    }

    [Fact]
    public void Price_delta_uses_authoritative_snapshots_without_currency_change()
    {
        var current = Snapshot(100_000m, 2);
        var proposed = Snapshot(110_000m, 2);

        var delta = BookingAmendmentPolicy.CalculatePriceDelta(
            current,
            proposed,
            2);

        Assert.Equal(20_000m, delta);
    }

    [Fact]
    public void Price_delta_rejects_currency_change()
    {
        var current = Snapshot(100_000m, 2, "INR");
        var proposed = Snapshot(100_000m, 2, "USD");

        Assert.Throws<InvalidOperationException>(() =>
            BookingAmendmentPolicy.CalculatePriceDelta(current, proposed, 2));
    }

    [Fact]
    public void Confirmation_rejects_stale_booking_version()
    {
        var proposal = Proposal(
            BookingOccupancy.Double,
            Traveller("First Traveller", 1),
            Traveller("Second Traveller", 2));
        var commercial = new BookingAmendmentCommercialSnapshot(
            Guid.NewGuid(),
            Snapshot(100_000m, 2));
        var preview = new BookingAmendmentPreview(
            Guid.NewGuid(),
            7,
            BookingOccupancy.Double,
            proposal.Travellers,
            Snapshot(100_000m, 2),
            proposal,
            commercial,
            0m,
            "fingerprint",
            new DateTimeOffset(2026, 8, 7, 12, 10, 0, TimeSpan.Zero));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BookingAmendmentPolicy.ValidatePreviewForConfirmation(
                preview,
                8,
                "fingerprint",
                new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero)));

        Assert.Contains("changed", exception.Message);
    }

    [Fact]
    public void Confirmation_rejects_expired_preview()
    {
        var proposal = Proposal(
            BookingOccupancy.Double,
            Traveller("First Traveller", 1),
            Traveller("Second Traveller", 2));
        var preview = new BookingAmendmentPreview(
            Guid.NewGuid(),
            4,
            BookingOccupancy.Double,
            proposal.Travellers,
            Snapshot(100_000m, 2),
            proposal,
            new BookingAmendmentCommercialSnapshot(
                Guid.NewGuid(),
                Snapshot(100_000m, 2)),
            0m,
            "fingerprint",
            new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero));

        Assert.Throws<InvalidOperationException>(() =>
            BookingAmendmentPolicy.ValidatePreviewForConfirmation(
                preview,
                4,
                "fingerprint",
                new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero)));
    }

    private static BookingAmendmentProposal Proposal(
        BookingOccupancy occupancy,
        params BookingAmendmentTraveller[] travellers) =>
        new(occupancy, travellers, "Correct the booked party.");

    private static BookingAmendmentTraveller Traveller(string name, int position) =>
        new(Guid.NewGuid(), position, name, new DateOnly(1990, 1, position));

    private static BookingFinancialSnapshot Snapshot(
        decimal unitPrice,
        int travellerCount,
        string currency = "INR")
    {
        var total = unitPrice * travellerCount;
        var dueNow = total / 5m;
        var remaining = total - dueNow;
        return new BookingFinancialSnapshot(
            currency,
            unitPrice,
            total,
            dueNow,
            remaining,
            [new BookingInstalment(1, new DateOnly(2026, 10, 5), remaining)]);
    }
}
