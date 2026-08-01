using NoorPath.Inventory;
using Xunit;

namespace NoorPath.Inventory.Tests;

public sealed class InventoryHoldPolicyTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 1, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Uses_inventory_lifetime_when_quote_remains_valid_longer()
    {
        var result = InventoryHoldPolicy.CalculateExpiry(
            CreatedAt,
            CreatedAt.AddMinutes(30),
            TimeSpan.FromMinutes(15));

        Assert.Equal(CreatedAt.AddMinutes(15), result);
    }

    [Fact]
    public void Never_extends_the_quote_expiry()
    {
        var result = InventoryHoldPolicy.CalculateExpiry(
            CreatedAt,
            CreatedAt.AddMinutes(7),
            TimeSpan.FromMinutes(15));

        Assert.Equal(CreatedAt.AddMinutes(7), result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_non_positive_lifetime(int minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InventoryHoldPolicy.CalculateExpiry(
                CreatedAt,
                CreatedAt.AddMinutes(30),
                TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void Rejects_a_quote_that_is_already_expired()
    {
        Assert.Throws<ArgumentException>(() =>
            InventoryHoldPolicy.CalculateExpiry(
                CreatedAt,
                CreatedAt,
                TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Exact_expiry_boundary_is_not_effectively_active()
    {
        Assert.False(InventoryHoldPolicy.IsEffectiveActive(
            InventoryHoldState.Active,
            CreatedAt.AddMinutes(15),
            CreatedAt.AddMinutes(15)));
        Assert.Equal(
            InventoryHoldState.Expired,
            InventoryHoldPolicy.EffectiveState(
                InventoryHoldState.Active,
                CreatedAt.AddMinutes(15),
                CreatedAt.AddMinutes(15)));
    }

    [Theory]
    [InlineData(InventoryHoldState.Released)]
    [InlineData(InventoryHoldState.Expired)]
    public void Terminal_states_remain_terminal(InventoryHoldState state)
    {
        Assert.Equal(
            state,
            InventoryHoldPolicy.EffectiveState(
                state,
                CreatedAt.AddMinutes(15),
                CreatedAt.AddHours(1)));
    }
}
