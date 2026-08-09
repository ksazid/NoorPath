using NoorPath.BuildingBlocks;

namespace NoorPath.Operators;

public enum OperatorState { Draft, PendingApproval, Approved, Rejected, Suspended, Deactivated }
public enum MembershipStatus { Active, Inactive }

public static class OperatorPermissions
{
    public const string AdminAccess = "operator.admin.access";
    public const string DocumentReview = "operator.documents.review";
    public const string VisaProcessing = "operator.visa.process";
    public const string OperationalSupport = "operator.support.manage";
}

public static class OperatorStatePolicy
{
    public static bool CanTransition(OperatorState from, OperatorState to) =>
        (from, to) switch
        {
            (OperatorState.Draft, OperatorState.PendingApproval) => true,
            (OperatorState.PendingApproval, OperatorState.Approved) => true,
            (OperatorState.PendingApproval, OperatorState.Rejected) => true,
            (OperatorState.Rejected, OperatorState.PendingApproval) => true,
            (OperatorState.Approved, OperatorState.Suspended) => true,
            (OperatorState.Approved, OperatorState.Deactivated) => true,
            (OperatorState.Suspended, OperatorState.Approved) => true,
            (OperatorState.Suspended, OperatorState.Deactivated) => true,
            _ => false
        };

    public static bool RequiresReason(OperatorState target) => target is
        OperatorState.Rejected or
        OperatorState.Suspended or
        OperatorState.Deactivated;
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

public sealed record OperatorPublicationEligibility(
    string OperatorId,
    OperatorState OperatorState,
    bool CanPublish);

public interface IOperatorPublicationEligibility
{
    Task<OperatorPublicationEligibility?> FindPublicationEligibilityAsync(
        string operatorId,
        CancellationToken cancellationToken);
}
