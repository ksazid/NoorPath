using Microsoft.EntityFrameworkCore;
using NoorPath.Inventory;

namespace NoorPath.Inventory.Infrastructure;

public static class InventoryAvailability
{
    public static async Task<IReadOnlyDictionary<Guid, int>> GetAvailableQuantitiesAsync(
        InventoryDbContext inventory,
        IReadOnlyCollection<InventoryPoolRecord> pools,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (pools.Count == 0)
            return new Dictionary<Guid, int>();

        var poolIds = pools.Select(pool => pool.Id).ToArray();
        var heldByPool = await inventory.Holds.AsNoTracking()
            .Where(hold =>
                poolIds.Contains(hold.InventoryPoolId) &&
                ((hold.State == InventoryHoldState.Active && hold.ExpiresAtUtc > nowUtc) ||
                 hold.State == InventoryHoldState.Committed))
            .GroupBy(hold => hold.InventoryPoolId)
            .Select(group => new
            {
                PoolId = group.Key,
                Quantity = group.Sum(hold => hold.Quantity)
            })
            .ToDictionaryAsync(item => item.PoolId, item => item.Quantity, cancellationToken);

        return pools.ToDictionary(
            pool => pool.Id,
            pool => Math.Max(0, pool.Capacity - heldByPool.GetValueOrDefault(pool.Id)));
    }

    public static async Task<int> GetCommittedQuantityAsync(
        InventoryDbContext inventory,
        Guid poolId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        await inventory.Holds.AsNoTracking()
            .Where(hold =>
                hold.InventoryPoolId == poolId &&
                ((hold.State == InventoryHoldState.Active && hold.ExpiresAtUtc > nowUtc) ||
                 hold.State == InventoryHoldState.Committed))
            .SumAsync(hold => (int?)hold.Quantity, cancellationToken) ?? 0;

    public static Task<int> MaterializeExpiredAsync(
        InventoryDbContext inventory,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken,
        Guid? poolId = null) =>
        inventory.Holds
            .Where(hold =>
                hold.State == InventoryHoldState.Active &&
                hold.ExpiresAtUtc <= nowUtc &&
                (poolId == null || hold.InventoryPoolId == poolId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(hold => hold.State, InventoryHoldState.Expired)
                    .SetProperty(hold => hold.TerminalAtUtc, nowUtc),
                cancellationToken);
}
