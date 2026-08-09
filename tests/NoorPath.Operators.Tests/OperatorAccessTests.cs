using Xunit;

namespace NoorPath.Operators.Tests;

public sealed class OperatorAccessTests
{
    [Theory]
    [InlineData(OperatorState.Draft)]
    [InlineData(OperatorState.PendingApproval)]
    [InlineData(OperatorState.Rejected)]
    [InlineData(OperatorState.Suspended)]
    [InlineData(OperatorState.Deactivated)]
    public void Ineligible_operator_state_is_denied(OperatorState state)
    {
        var access = new OperatorAccess(
            "operator",
            "Operator",
            state,
            new HashSet<string> { OperatorPermissions.AdminAccess });

        Assert.False(access.IsAllowed(OperatorPermissions.AdminAccess));
    }

    [Fact]
    public void Approved_operator_requires_the_explicit_permission()
    {
        var denied = new OperatorAccess(
            "operator",
            "Operator",
            OperatorState.Approved,
            new HashSet<string>());
        var allowed = denied with
        {
            Permissions = new HashSet<string> { OperatorPermissions.AdminAccess }
        };

        Assert.False(denied.IsAllowed(OperatorPermissions.AdminAccess));
        Assert.True(allowed.IsAllowed(OperatorPermissions.AdminAccess));
    }

    [Theory]
    [InlineData(OperatorState.Draft, OperatorState.PendingApproval)]
    [InlineData(OperatorState.PendingApproval, OperatorState.Approved)]
    [InlineData(OperatorState.PendingApproval, OperatorState.Rejected)]
    [InlineData(OperatorState.Rejected, OperatorState.PendingApproval)]
    [InlineData(OperatorState.Approved, OperatorState.Suspended)]
    [InlineData(OperatorState.Approved, OperatorState.Deactivated)]
    [InlineData(OperatorState.Suspended, OperatorState.Approved)]
    [InlineData(OperatorState.Suspended, OperatorState.Deactivated)]
    public void Governed_operator_transitions_are_allowed(
        OperatorState from,
        OperatorState to)
    {
        Assert.True(OperatorStatePolicy.CanTransition(from, to));
    }

    [Theory]
    [InlineData(OperatorState.Draft, OperatorState.Approved)]
    [InlineData(OperatorState.PendingApproval, OperatorState.Suspended)]
    [InlineData(OperatorState.Rejected, OperatorState.Approved)]
    [InlineData(OperatorState.Deactivated, OperatorState.Approved)]
    [InlineData(OperatorState.Approved, OperatorState.PendingApproval)]
    public void Lifecycle_shortcuts_are_denied(
        OperatorState from,
        OperatorState to)
    {
        Assert.False(OperatorStatePolicy.CanTransition(from, to));
    }

    [Theory]
    [InlineData(OperatorState.Rejected)]
    [InlineData(OperatorState.Suspended)]
    [InlineData(OperatorState.Deactivated)]
    public void Adverse_decisions_require_a_reason(OperatorState target)
    {
        Assert.True(OperatorStatePolicy.RequiresReason(target));
    }
}
