using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Testing;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class CommercialAuthoringApiTests
{
    [Fact]
    public async Task Authorized_operator_can_configure_pricing_and_inventory_for_own_draft()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        var departureId = await CreateDraftAsync(client, TestContext.Current.CancellationToken);

        var pricing = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, pricing.StatusCode);

        var inventory = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            ValidInventory(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, inventory.StatusCode);

        var get = await client.GetAsync(
            $"/api/v1/operator/departures/{departureId}/commercial",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        using var body = JsonDocument.Parse(await get.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("INR", body.RootElement.GetProperty("pricing").GetProperty("currency").GetString());
        var readiness = body.RootElement.GetProperty("readiness").EnumerateArray().ToArray();
        Assert.True(readiness.Single(x => x.GetProperty("occupancy").GetString() == "double").GetProperty("ready").GetBoolean());
        Assert.True(readiness.Single(x => x.GetProperty("occupancy").GetString() == "triple").GetProperty("ready").GetBoolean());
        Assert.False(readiness.Single(x => x.GetProperty("occupancy").GetString() == "quad").GetProperty("ready").GetBoolean());

        using var scope = app.Services.CreateScope();
        var pricingDb = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        Assert.Single(await pricingDb.Audits.ToListAsync(TestContext.Current.CancellationToken));
        var inventoryAudit = Assert.Single(await inventoryDb.Audits.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Initial sellable allocation", inventoryAudit.Reason);
    }

    [Fact]
    public async Task Unauthenticated_and_permissionless_access_is_denied()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = Guid.NewGuid();
        using var anonymous = app.CreateClient();
        using var permissionless = app.CreateOperatorClient(CommercialApi.NoPermissionIdentity);

        var unauthenticated = await anonymous.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal("application/problem+json", unauthenticated.Content.Headers.ContentType?.MediaType);

        var forbidden = await permissionless.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Cross_operator_commercial_configuration_is_hidden()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        using var author = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        using var other = app.CreateOperatorClient(CommercialApi.OtherIdentity);
        var departureId = await CreateDraftAsync(author, TestContext.Current.CancellationToken);

        var get = await other.GetAsync(
            $"/api/v1/operator/departures/{departureId}/commercial",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        var put = await other.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            ValidInventory(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Fact]
    public async Task Invalid_pricing_and_inventory_return_validation_problems_without_persistence()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        var departureId = await CreateDraftAsync(client, TestContext.Current.CancellationToken);

        var invalidPricing = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            new SavePricingRequest(0, "12", [new("single", 0)]),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidPricing.StatusCode);

        var invalidInventory = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            new SaveInventoryRequest(0, "", [new("double", -1)]),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidInventory.StatusCode);

        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<PricingDbContext>().PricePlans.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Configurations.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Stale_capability_versions_do_not_overwrite_newer_configuration()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        var departureId = await CreateDraftAsync(client, TestContext.Current.CancellationToken);

        (await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            ValidInventory(),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var stalePricing = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, stalePricing.StatusCode);
        using (var problem = JsonDocument.Parse(await stalePricing.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal("stale_pricing_version", problem.RootElement.GetProperty("code").GetString());

        var staleInventory = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            ValidInventory(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, staleInventory.StatusCode);
        using (var problem = JsonDocument.Parse(await staleInventory.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal("stale_inventory_version", problem.RootElement.GetProperty("code").GetString());

        using var scope = app.Services.CreateScope();
        var pricingDb = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        Assert.Single(await pricingDb.Audits.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await inventoryDb.Audits.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, (await pricingDb.PricePlans.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).Version);
        Assert.Equal(1, (await inventoryDb.Configurations.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).Version);
    }

    private static SavePricingRequest ValidPricing() => new(
        0,
        "inr",
        [
            new("double", 110000m),
            new("triple", 100000m),
            new("quad", 90000m)
        ]);

    private static SaveInventoryRequest ValidInventory() => new(
        0,
        "Initial sellable allocation",
        [
            new("double", 10),
            new("triple", 8),
            new("quad", 0)
        ]);

    private static async Task<Guid> CreateDraftAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/operator/departures",
            new SaveCatalogueDraftRequest(
                "Noor Harmony 12 Nights",
                "A guided Umrah journey with factual package information.",
                new("Makkah Hotel", "4 star", "850 m from Masjid al-Haram", 6, "confirmed"),
                new("Madinah Hotel", "4 star", "450 m from Al-Masjid an-Nabawi", 5, "pending"),
                new("Delhi → Jeddah → Makkah → Madinah", "Flight details pending final confirmation.", "pending"),
                "Delhi (DEL)",
                new(2026, 10, 10),
                new(2026, 10, 22),
                ["Return flights", "Breakfast"],
                ["Personal expenses"]),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<DraftResponse>(cancellationToken: cancellationToken))!;
        return created.DepartureId;
    }

    private sealed record DraftResponse(Guid DepartureId);
}

public sealed class CommercialApi : WebApplicationFactory<Program>
{
    public const string AuthorIdentity = "commercial-author";
    public const string OtherIdentity = "commercial-other";
    public const string NoPermissionIdentity = "commercial-no-permission";

    private readonly string connection;
    private CommercialApi(string connection) => this.connection = connection;

    public static async Task<CommercialApi> CreateAsync(CancellationToken cancellationToken)
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_COMMERCIAL_TEST_DB",
            "Commercial API");
        var app = new CommercialApi(connection);

        using var scope = app.Services.CreateScope();
        var catalogue = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        var operators = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        var pricing = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await catalogue.Database.EnsureDeletedAsync(cancellationToken);
        await operators.Database.MigrateAsync(cancellationToken);
        await catalogue.Database.MigrateAsync(cancellationToken);
        await pricing.Database.MigrateAsync(cancellationToken);
        await inventory.Database.MigrateAsync(cancellationToken);
        await SeedOperatorsAsync(operators, cancellationToken);
        return app;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestSettings.ConfigureTestHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogueDbContext>>();
            services.RemoveAll<CatalogueDbContext>();
            services.RemoveAll<DbContextOptions<OperatorsDbContext>>();
            services.RemoveAll<OperatorsDbContext>();
            services.RemoveAll<DbContextOptions<PricingDbContext>>();
            services.RemoveAll<PricingDbContext>();
            services.RemoveAll<DbContextOptions<InventoryDbContext>>();
            services.RemoveAll<InventoryDbContext>();

            services.AddDbContext<CatalogueDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(CatalogueDbContext).Assembly.FullName)));
            services.AddDbContext<OperatorsDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(OperatorsDbContext).Assembly.FullName)));
            services.AddDbContext<PricingDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(PricingDbContext).Assembly.FullName)));
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName)));
        });
    }

    public HttpClient CreateOperatorClient(string identity)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-NoorPath-Test-Identity", identity);
        return client;
    }

    private static async Task SeedOperatorsAsync(OperatorsDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var operatorA = new OperatorRecord
        {
            Id = "operator-noor",
            DisplayName = "Noor International Tours & Travels",
            State = OperatorState.Approved,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var operatorB = new OperatorRecord
        {
            Id = "operator-rahma",
            DisplayName = "Rahma Pilgrimage Services",
            State = OperatorState.Approved,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var authorMembership = Membership(operatorA.Id, AuthorIdentity, now);
        var otherMembership = Membership(operatorB.Id, OtherIdentity, now);
        var noPermissionMembership = Membership(operatorA.Id, NoPermissionIdentity, now);

        db.AddRange(operatorA, operatorB, authorMembership, otherMembership, noPermissionMembership);
        db.MembershipPermissions.AddRange(Permission(authorMembership.Id), Permission(otherMembership.Id));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static OperatorMembershipRecord Membership(string operatorId, string accountId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        OperatorId = operatorId,
        AccountId = accountId,
        Status = MembershipStatus.Active,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static OperatorMembershipPermissionRecord Permission(Guid membershipId) => new()
    {
        Id = Guid.NewGuid(),
        MembershipId = membershipId,
        Permission = OperatorPermissions.AdminAccess
    };
}
