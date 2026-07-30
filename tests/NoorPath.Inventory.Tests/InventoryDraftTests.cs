using NoorPath.Inventory;
using Xunit;

namespace NoorPath.Inventory.Tests;

public sealed class InventoryDraftTests
{
    [Fact]
    public void Trims_reason_and_orders_supported_occupancies()
    {
        var draft = new InventoryDraft(new(
            " Initial allocation ",
            [
                new(InventoryOccupancy.Quad, 6),
                new(InventoryOccupancy.Double, 10),
                new(InventoryOccupancy.Triple, 8)
            ]));

        Assert.Equal("Initial allocation", draft.Details.AdjustmentReason);
        Assert.Equal(
            [InventoryOccupancy.Double, InventoryOccupancy.Triple, InventoryOccupancy.Quad],
            draft.Details.Pools.Select(x => x.Occupancy));
    }

    [Fact]
    public void Allows_zero_capacity_for_a_configured_but_unavailable_occupancy()
    {
        var draft = new InventoryDraft(new(
            "Pause this occupancy",
            [new(InventoryOccupancy.Double, 0)]));

        Assert.Equal(0, draft.Details.Pools.Single().Capacity);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("Reason", -1)]
    public void Rejects_missing_reason_or_negative_capacity(string reason, int capacity)
    {
        Assert.Throws<InventoryDraftValidationException>(() => new InventoryDraft(new(
            reason,
            [new(InventoryOccupancy.Double, capacity)])));
    }

    [Fact]
    public void Rejects_duplicate_occupancy()
    {
        Assert.Throws<InventoryDraftValidationException>(() => new InventoryDraft(new(
            "Duplicate test",
            [
                new(InventoryOccupancy.Double, 4),
                new(InventoryOccupancy.Double, 5)
            ])));
    }
}
