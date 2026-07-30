namespace NoorPath.Inventory;

public enum InventoryOccupancy
{
    Double,
    Triple,
    Quad
}

public sealed record InventoryPoolDraft(
    InventoryOccupancy Occupancy,
    int Capacity);

public sealed record InventoryDraftDetails(
    string AdjustmentReason,
    IReadOnlyList<InventoryPoolDraft> Pools);

public sealed class InventoryDraft
{
    public InventoryDraftDetails Details { get; }

    public InventoryDraft(InventoryDraftDetails details)
    {
        var errors = Validate(details);
        if (errors.Count != 0)
            throw new InventoryDraftValidationException(errors);

        Details = details with
        {
            AdjustmentReason = details.AdjustmentReason.Trim(),
            Pools = details.Pools
                .OrderBy(x => x.Occupancy)
                .ToArray()
        };
    }

    public static Dictionary<string, string[]> Validate(InventoryDraftDetails value)
    {
        var errors = new Dictionary<string, string[]>();
        var reason = value.AdjustmentReason?.Trim() ?? string.Empty;

        if (reason.Length == 0)
            errors["adjustmentReason"] = ["Adjustment reason is required."];
        else if (reason.Length > 240)
            errors["adjustmentReason"] = ["Adjustment reason must be 240 characters or fewer."];

        if (value.Pools.Count is < 1 or > 3)
            errors["pools"] = ["Configure between one and three supported occupancies."];

        if (value.Pools.GroupBy(x => x.Occupancy).Any(group => group.Count() > 1))
            errors["pools"] = ["Each occupancy can be configured only once."];

        for (var index = 0; index < value.Pools.Count; index++)
        {
            if (value.Pools[index].Capacity < 0)
                errors[$"pools[{index}].capacity"] = ["Capacity cannot be negative."];
        }

        return errors;
    }
}

public sealed class InventoryDraftValidationException(Dictionary<string, string[]> errors)
    : Exception("Inventory draft validation failed.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
