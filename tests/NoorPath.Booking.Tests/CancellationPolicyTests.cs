using NoorPath.Booking;
using Xunit;

namespace NoorPath.Booking.Tests;

public sealed class CancellationPolicyTests
{
    [Fact]
    public void Evaluate_uses_matching_window_and_explainable_fee_components()
    {
        var policy = Policy(
            new CancellationWindowPolicy(30, 1_000, 500m),
            new CancellationWindowPolicy(0, 5_000, 1_000m));

        var result = CancellationPolicy.Evaluate(
            policy,
            new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
            new DateOnly(2026, 9, 10),
            "INR",
            10_000m);

        Assert.True(result.IsEligible);
        Assert.NotNull(result.Entitlement);
        Assert.Equal(40, result.Entitlement.DaysBeforeDeparture);
        Assert.Equal(1_000, result.Entitlement.FeeBasisPoints);
        Assert.Equal(1_000m, result.Entitlement.PercentageFee);
        Assert.Equal(500m, result.Entitlement.NonRefundableAmount);
        Assert.Equal(8_500m, result.Entitlement.RefundableAmount);
        Assert.Equal(2, result.Entitlement.FeeComponents.Count);
    }

    [Fact]
    public void Evaluate_caps_fees_at_authoritative_settled_amount()
    {
        var policy = Policy(new CancellationWindowPolicy(0, 10_000, 5_000m));

        var result = CancellationPolicy.Evaluate(
            policy,
            new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
            new DateOnly(2026, 8, 2),
            "INR",
            3_000m);

        Assert.True(result.IsEligible);
        Assert.Equal(3_000m, result.Entitlement!.PercentageFee);
        Assert.Equal(0m, result.Entitlement.NonRefundableAmount);
        Assert.Equal(0m, result.Entitlement.RefundableAmount);
    }

    [Fact]
    public void Evaluate_rejects_requests_after_the_configured_cutoff()
    {
        var policy = Policy(new CancellationWindowPolicy(0, 0, 0m));

        var result = CancellationPolicy.Evaluate(
            policy,
            new DateTimeOffset(2026, 8, 2, 10, 1, 0, TimeSpan.Zero),
            new DateOnly(2026, 8, 2),
            "INR",
            1_000m);

        Assert.False(result.IsEligible);
        Assert.Equal("departure_started", result.Code);
        Assert.Null(result.Entitlement);
    }

    [Fact]
    public void Validate_rejects_duplicate_window_thresholds()
    {
        var policy = Policy(
            new CancellationWindowPolicy(30, 1_000, 0m),
            new CancellationWindowPolicy(30, 2_000, 0m));

        Assert.Throws<ArgumentException>(() => CancellationPolicy.Validate(policy));
    }

    [Fact]
    public void Confirmed_booking_can_transition_to_cancelled_but_exception_cannot()
    {
        Assert.True(BookingPolicy.CanTransition(BookingState.Confirmed, BookingState.Cancelled));
        Assert.False(BookingPolicy.CanTransition(BookingState.ConfirmationException, BookingState.Cancelled));
    }

    private static CancellationPolicyDefinition Policy(
        params CancellationWindowPolicy[] windows) =>
        new(
            "vs16-test-v1",
            "UTC",
            new TimeOnly(10, 0),
            10,
            windows);
}
