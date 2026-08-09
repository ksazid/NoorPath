using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using Xunit;

namespace NoorPath.Operators.Integration.Tests;

public sealed class PlatformOperatorAdministrationApiTests
{
    [Fact]
    public async Task Platform_administrator_can_approve_pending_operator_and_unlock_operator_access()
    {
        using var app = await OperatorApi.CreateAsync();
        await SeedPendingOperatorAsync(app, "barakah", "Barakah Umrah", "pending-operator");

        using var operatorClient = app.CreateClientFor("pending-operator");
        var before = await operatorClient.GetAsync(
            "/api/v1/operator/access",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

        using var administrator = app.CreateClientFor("platform-administrator");
        var summary = await administrator.GetAsync(
            "/api/v1/platform/operators/summary",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.Contains(
            "Barakah Umrah",
            await summary.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var decision = await administrator.PostAsJsonAsync(
            "/api/v1/platform/operators/barakah/state",
            new
            {
                targetState = "approved",
                expectedVersion = 1,
                reason = "Operator verification completed."
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);

        var after = await operatorClient.GetAsync(
            "/api/v1/operator/access",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        var audit = await db.StateAudits.AsNoTracking().SingleAsync(
            item => item.OperatorId == "barakah",
            TestContext.Current.CancellationToken);

        Assert.Equal(OperatorState.PendingApproval, audit.FromState);
        Assert.Equal(OperatorState.Approved, audit.ToState);
        Assert.Equal("platform-administrator", audit.ActorAccountId);
        Assert.Equal(2, audit.OperatorVersion);
    }

    [Fact]
    public async Task Non_platform_identity_cannot_view_or_change_operator_lifecycle()
    {
        using var app = await OperatorApi.CreateAsync();
        await SeedPendingOperatorAsync(app, "barakah", "Barakah Umrah", "pending-operator");
        using var customer = app.CreateClientFor("customer-account");

        var list = await customer.GetAsync(
            "/api/v1/platform/operators",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        var decision = await customer.PostAsJsonAsync(
            "/api/v1/platform/operators/barakah/state",
            new
            {
                targetState = "approved",
                expectedVersion = 1,
                reason = "Should not be accepted."
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, decision.StatusCode);
    }

    [Fact]
    public async Task Adverse_decisions_require_reason_and_stale_versions_are_rejected()
    {
        using var app = await OperatorApi.CreateAsync();
        await SeedPendingOperatorAsync(app, "barakah", "Barakah Umrah", "pending-operator");
        using var administrator = app.CreateClientFor("platform-administrator");

        var withoutReason = await administrator.PostAsJsonAsync(
            "/api/v1/platform/operators/barakah/state",
            new
            {
                targetState = "rejected",
                expectedVersion = 1,
                reason = ""
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutReason.StatusCode);

        var rejected = await administrator.PostAsJsonAsync(
            "/api/v1/platform/operators/barakah/state",
            new
            {
                targetState = "rejected",
                expectedVersion = 1,
                reason = "Business verification needs correction."
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);

        var stale = await administrator.PostAsJsonAsync(
            "/api/v1/platform/operators/barakah/state",
            new
            {
                targetState = "pendingApproval",
                expectedVersion = 1,
                reason = "Resubmitted."
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains(
            "stale_operator_decision",
            await stale.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static async Task SeedPendingOperatorAsync(
        OperatorApi app,
        string operatorId,
        string displayName,
        string accountId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        var now = DateTimeOffset.UtcNow;
        var membership = new OperatorMembershipRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            AccountId = accountId,
            Status = MembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Operators.Add(new OperatorRecord
        {
            Id = operatorId,
            DisplayName = displayName,
            State = OperatorState.PendingApproval,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Memberships.Add(membership);
        db.MembershipPermissions.Add(new OperatorMembershipPermissionRecord
        {
            Id = Guid.NewGuid(),
            MembershipId = membership.Id,
            Permission = OperatorPermissions.AdminAccess
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
