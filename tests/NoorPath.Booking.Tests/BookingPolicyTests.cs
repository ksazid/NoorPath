using NoorPath.Booking;
using Xunit;

namespace NoorPath.Booking.Tests;

public sealed class BookingPolicyTests
{
    [Theory]
    [InlineData(BookingOccupancy.Double, 2)]
    [InlineData(BookingOccupancy.Triple, 3)]
    [InlineData(BookingOccupancy.Quad, 4)]
    public void Occupancy_requires_exact_traveller_count(
        BookingOccupancy occupancy,
        int expected)
    {
        Assert.Equal(expected, BookingPolicy.RequiredTravellerCount(occupancy));
    }

    [Fact]
    public void Valid_snapshot_preserves_authoritative_arithmetic()
    {
        var snapshot = new BookingFinancialSnapshot(
            "INR",
            100_000m,
            200_000m,
            40_000m,
            160_000m,
            [
                new BookingInstalment(1, new DateOnly(2026, 10, 5), 80_000m),
                new BookingInstalment(2, new DateOnly(2026, 11, 5), 80_000m)
            ]);

        BookingPolicy.ValidateSnapshot(snapshot, 2);
    }

    [Fact]
    public void Snapshot_rejects_recomputed_or_incomplete_money()
    {
        var snapshot = new BookingFinancialSnapshot(
            "INR",
            100_000m,
            199_999m,
            40_000m,
            159_999m,
            [new BookingInstalment(1, new DateOnly(2026, 10, 5), 159_999m)]);

        Assert.Throws<ArgumentException>(() =>
            BookingPolicy.ValidateSnapshot(snapshot, 2));
    }

    [Fact]
    public void Payment_success_is_terminal_inside_vs09()
    {
        Assert.True(BookingPolicy.CanTransition(
            BookingState.PaymentInProgress,
            BookingState.PaymentSucceeded));
        Assert.False(BookingPolicy.CanTransition(
            BookingState.PaymentSucceeded,
            BookingState.PaymentFailed));
    }
}
