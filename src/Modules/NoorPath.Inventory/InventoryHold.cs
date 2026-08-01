namespace NoorPath.Inventory;

public enum InventoryHoldState
{
    Active,
    Released,
    Expired,
    Committed
}

public sealed class InventoryHoldOptions
{
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(15);
}

public static class InventoryHoldPolicy
{
    public static DateTimeOffset CalculateExpiry(
        DateTimeOffset createdAtUtc,
        DateTimeOffset quoteExpiresAtUtc,
        TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Hold lifetime must be positive.");

        if (quoteExpiresAtUtc <= createdAtUtc)
            throw new ArgumentException("Quote must still be valid when a hold is created.", nameof(quoteExpiresAtUtc));

        var policyExpiry = createdAtUtc.Add(lifetime);
        return policyExpiry <= quoteExpiresAtUtc ? policyExpiry : quoteExpiresAtUtc;
    }

    public static bool IsEffectiveActive(
        InventoryHoldState state,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc) =>
        state == InventoryHoldState.Active && expiresAtUtc > nowUtc;

    public static InventoryHoldState EffectiveState(
        InventoryHoldState state,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc) =>
        state == InventoryHoldState.Active && expiresAtUtc <= nowUtc
            ? InventoryHoldState.Expired
            : state;
}
