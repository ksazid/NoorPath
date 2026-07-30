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
        var access = new OperatorAccess("operator", "Operator", state, new HashSet<string> { OperatorPermissions.AdminAccess });
        Assert.False(access.IsAllowed(OperatorPermissions.AdminAccess));
    }

    [Fact]
    public void Approved_operator_requires_the_explicit_permission()
    {
        var denied = new OperatorAccess("operator", "Operator", OperatorState.Approved, new HashSet<string>());
        var allowed = denied with { Permissions = new HashSet<string> { OperatorPermissions.AdminAccess } };
        Assert.False(denied.IsAllowed(OperatorPermissions.AdminAccess));
        Assert.True(allowed.IsAllowed(OperatorPermissions.AdminAccess));
    }
}
