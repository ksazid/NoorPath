using NoorPath.Booking;
using Xunit;

namespace NoorPath.Booking.Tests;

public sealed class DepartureHandoverPolicyTests
{
    [Fact]
    public void Completion_is_blocked_when_any_traveller_is_blocked()
    {
        var result = DepartureHandoverPolicy.ValidateCompletion(10, 1, "Ready for departure", 0, 0, false);

        Assert.Equal("handover_blocked", result);
    }

    [Fact]
    public void Completion_requires_at_least_one_traveller()
    {
        var result = DepartureHandoverPolicy.ValidateCompletion(0, 0, "Ready", 0, 0, false);

        Assert.Equal("handover_empty", result);
    }

    [Fact]
    public void Completion_rejects_stale_version()
    {
        var result = DepartureHandoverPolicy.ValidateCompletion(10, 0, "Ready", 1, 2, false);

        Assert.Equal("handover_stale", result);
    }

    [Fact]
    public void Completion_requires_a_bounded_note()
    {
        Assert.Equal(
            "handover_note_invalid",
            DepartureHandoverPolicy.ValidateCompletion(10, 0, "   ", 0, 0, false));
        Assert.Equal(
            "handover_note_invalid",
            DepartureHandoverPolicy.ValidateCompletion(10, 0, new string('x', 501), 0, 0, false));
    }

    [Fact]
    public void Completion_is_allowed_when_every_gate_is_clear()
    {
        var result = DepartureHandoverPolicy.ValidateCompletion(10, 0, "All pilgrims handed over", 0, 0, false);

        Assert.Null(result);
        Assert.True(DepartureHandoverPolicy.CanComplete(10, 0));
    }

    [Fact]
    public void Already_completed_handover_is_idempotent()
    {
        var result = DepartureHandoverPolicy.ValidateCompletion(10, 4, null, 99, 1, true);

        Assert.Null(result);
    }
}
