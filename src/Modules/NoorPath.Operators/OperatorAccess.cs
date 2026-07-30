using NoorPath.BuildingBlocks;

namespace NoorPath.Operators;

public enum OperatorState { Draft, PendingApproval, Approved, Rejected, Suspended, Deactivated }
public enum MembershipStatus { Active, Inactive }

public static class OperatorPermissions
{
    public const string AdminAccess = "operator.admin.access";
}

public sealed record OperatorAccess(
    string OperatorId,
    string OperatorDisplayName,
    OperatorState OperatorState,
    IReadOnlySet<string> Permissions)
{
    public bool IsAllowed(string permission) =>
        OperatorState == OperatorState.Approved && Permissions.Contains(permission);
}

public interface IOperatorAccess
{
    Task<OperatorAccess?> FindActiveMembershipAsync(AccountId accountId, CancellationToken cancellationToken);
}
