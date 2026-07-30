namespace NoorPath.BuildingBlocks;

public readonly record struct AccountId(string Value)
{
    public override string ToString() => Value;
}
