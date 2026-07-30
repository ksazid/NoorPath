using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Testing;
using Xunit;

namespace NoorPath.Operators.Integration.Tests;

public sealed class OperatorAccessApiTests
{
    [Fact]
    public void Test_authentication_fails_closed_in_production()
    {
        using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("Authentication:Mode", "Test");
            });

        Assert.ThrowsAny<Exception>(() => _ = app.Services);
    }

    [Fact]
    public async Task Access_is_unauthenticated_without_test_identity()
    {
        using var app = await OperatorApi.CreateAsync();

        var response = await app.CreateClient().GetAsync(
            "/api/v1/operator/access",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Theory]
    [InlineData("unknown-account")]
    [InlineData("without-permission")]
    [InlineData("suspended-account")]
    public async Task Access_is_forbidden_without_complete_authority(
        string accountId)
    {
        using var app = await OperatorApi.CreateAsync();
        using var client = app.CreateClientFor(accountId);

        var response = await client.GetAsync(
            "/api/v1/operator/access",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Approved_permitted_member_receives_only_their_operator_context()
    {
        using var app = await OperatorApi.CreateAsync();
        using var client = app.CreateClientFor("approved-account");

        client.DefaultRequestHeaders.Add("X-Operator-Id", "rahma");

        var response = await client.GetAsync(
            "/api/v1/operator/access",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.Contains("Noor Tours", body);
        Assert.Contains(OperatorPermissions.AdminAccess, body);
        Assert.DoesNotContain("Rahma Tours", body);
        Assert.DoesNotContain("noorpath_account_id", body);
    }
}

public sealed class OperatorApi : WebApplicationFactory<Program>
{
    private readonly string connection;

    private OperatorApi(string connection)
    {
        this.connection = connection;
    }

    public static async Task<OperatorApi> CreateAsync()
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_OPERATORS_TEST_DB",
            "Operators");

        var app = new OperatorApi(connection);

        using var scope = app.Services.CreateScope();

        var db =
            scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();

        await db.Database.EnsureDeletedAsync(
            TestContext.Current.CancellationToken);

        await db.Database.MigrateAsync(
            TestContext.Current.CancellationToken);

        await SeedAsync(
            db,
            TestContext.Current.CancellationToken);

        return app;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestSettings.ConfigureTestHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OperatorsDbContext>>();
            services.RemoveAll<OperatorsDbContext>();

            services.AddDbContext<OperatorsDbContext>(
                options =>
                    options.UseNpgsql(
                        connection,
                        postgres =>
                            postgres.MigrationsAssembly(
                                typeof(OperatorsDbContext)
                                    .Assembly
                                    .FullName)));
        });
    }

    public HttpClient CreateClientFor(string accountId)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-NoorPath-Test-Identity",
            accountId);

        return client;
    }

    private static async Task SeedAsync(
        OperatorsDbContext db,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        db.Operators.AddRange(
            new OperatorRecord
            {
                Id = "noor",
                DisplayName = "Noor Tours",
                State = OperatorState.Approved,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new OperatorRecord
            {
                Id = "rahma",
                DisplayName = "Rahma Tours",
                State = OperatorState.Suspended,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        var approved = new OperatorMembershipRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = "noor",
            AccountId = "approved-account",
            Status = MembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var noPermission = new OperatorMembershipRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = "noor",
            AccountId = "without-permission",
            Status = MembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var suspended = new OperatorMembershipRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = "rahma",
            AccountId = "suspended-account",
            Status = MembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Memberships.AddRange(
            approved,
            noPermission,
            suspended);

        db.MembershipPermissions.AddRange(
            new OperatorMembershipPermissionRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = approved.Id,
                Permission = OperatorPermissions.AdminAccess
            },
            new OperatorMembershipPermissionRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = suspended.Id,
                Permission = OperatorPermissions.AdminAccess
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
