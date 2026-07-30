using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Testing;
using Xunit;

namespace NoorPath.Catalogue.Integration.Tests;

public sealed class CatalogueApiTests
{
    [Fact]
    public async Task Authorized_operator_can_create_load_and_update_own_draft()
    {
        using var app = await CatalogueApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CatalogueApi.AuthorIdentity);

        var created = await CreateDraftAsync(client, ValidDraft(), TestContext.Current.CancellationToken);
        Assert.Equal(1, created.Version);
        Assert.Equal("draft", created.Status);

        var get = await client.GetAsync($"/api/v1/operator/departures/{created.DepartureId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = await get.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Noor Harmony 12 Nights", body);
        Assert.Contains("makkah", body);
        Assert.Contains("madinah", body);
        Assert.Contains("pending", body);
        Assert.DoesNotContain("operatorId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("price", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capacity", body, StringComparison.OrdinalIgnoreCase);

        var updatedDraft = ValidDraft() with { Summary = "Updated factual journey summary." };
        var update = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{created.DepartureId}",
            new UpdateCatalogueDraftRequest(created.Version, updatedDraft),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<DraftResponse>(cancellationToken: TestContext.Current.CancellationToken))!;
        Assert.Equal(2, updated.Version);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        Assert.Single(await db.PackageTemplates.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await db.PackageVersions.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await db.DepartureBatches.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await db.DraftAudits.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Unauthenticated_authoring_is_rejected_with_problem_details()
    {
        using var app = await CatalogueApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/operator/departures",
            ValidDraft(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Membership_without_admin_permission_cannot_author_catalogue()
    {
        using var app = await CatalogueApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CatalogueApi.NoPermissionIdentity);

        var response = await client.PostAsJsonAsync(
            "/api/v1/operator/departures",
            ValidDraft(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cross_operator_draft_is_hidden_for_reads_and_writes()
    {
        using var app = await CatalogueApi.CreateAsync(TestContext.Current.CancellationToken);
        using var author = app.CreateOperatorClient(CatalogueApi.AuthorIdentity);
        using var other = app.CreateOperatorClient(CatalogueApi.OtherIdentity);
        var created = await CreateDraftAsync(author, ValidDraft(), TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await other.GetAsync($"/api/v1/operator/departures/{created.DepartureId}", TestContext.Current.CancellationToken)).StatusCode);

        var update = await other.PutAsJsonAsync(
            $"/api/v1/operator/departures/{created.DepartureId}",
            new UpdateCatalogueDraftRequest(created.Version, ValidDraft()),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    [Fact]
    public async Task Invalid_draft_returns_validation_problem_without_persistence()
    {
        using var app = await CatalogueApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CatalogueApi.AuthorIdentity);
        var invalid = ValidDraft() with
        {
            PackageName = "",
            ReturnDate = new(2026, 10, 9),
            Makkah = ValidDraft().Makkah! with { HotelName = "" }
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/operator/departures",
            invalid,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("packageName", out _));
        Assert.True(errors.TryGetProperty("returnDate", out _));
        Assert.True(errors.TryGetProperty("makkah.hotelName", out _));

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        Assert.Empty(await db.DepartureBatches.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Stale_save_is_a_conflict_and_does_not_append_audit()
    {
        using var app = await CatalogueApi.CreateAsync(TestContext.Current.CancellationToken);
        using var client = app.CreateOperatorClient(CatalogueApi.AuthorIdentity);
        var created = await CreateDraftAsync(client, ValidDraft(), TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/operator/departures/{created.DepartureId}",
            new UpdateCatalogueDraftRequest(created.Version + 1, ValidDraft()),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("stale_version", problem.RootElement.GetProperty("code").GetString());

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        Assert.Single(await db.DraftAudits.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, (await db.DepartureBatches.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).Version);
    }

    private static async Task<DraftResponse> CreateDraftAsync(
        HttpClient client,
        SaveCatalogueDraftRequest request,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync("/api/v1/operator/departures", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DraftResponse>(cancellationToken: cancellationToken))!;
    }

    private static SaveCatalogueDraftRequest ValidDraft() => new(
        "Noor Harmony 12 Nights",
        "A guided Umrah journey with factual package information.",
        new("Makkah Hotel", "4 star", "850 m from Masjid al-Haram", 6, "confirmed"),
        new("Madinah Hotel", "4 star", "450 m from Al-Masjid an-Nabawi", 5, "pending"),
        new("Delhi → Jeddah → Makkah → Madinah", "Flight details pending final confirmation.", "pending"),
        "Delhi (DEL)",
        new(2026, 10, 10),
        new(2026, 10, 22),
        ["Return flights", "Breakfast"],
        ["Personal expenses"]);

    private sealed record DraftResponse(
        Guid PackageTemplateId,
        Guid PackageVersionId,
        Guid DepartureId,
        int Version,
        string Status);
}

public sealed class CatalogueApi : WebApplicationFactory<Program>
{
    public const string AuthorIdentity = "catalogue-author";
    public const string OtherIdentity = "catalogue-other";
    public const string NoPermissionIdentity = "catalogue-no-permission";

    private readonly string connection;
    private CatalogueApi(string connection) => this.connection = connection;

    public static async Task<CatalogueApi> CreateAsync(CancellationToken cancellationToken)
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_CATALOGUE_TEST_DB",
            "Catalogue API");
        var app = new CatalogueApi(connection);

        using var scope = app.Services.CreateScope();
        var catalogue = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        var operators = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        await catalogue.Database.EnsureDeletedAsync(cancellationToken);
        await catalogue.Database.MigrateAsync(cancellationToken);
        await operators.Database.MigrateAsync(cancellationToken);
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

            services.AddDbContext<CatalogueDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(CatalogueDbContext).Assembly.FullName)));
            services.AddDbContext<OperatorsDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(OperatorsDbContext).Assembly.FullName)));
        });
    }

    public HttpClient CreateOperatorClient(string identity)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-NoorPath-Test-Identity", identity);
        return client;
    }

    private static async Task SeedOperatorsAsync(
        OperatorsDbContext db,
        CancellationToken cancellationToken)
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
        db.MembershipPermissions.AddRange(
            Permission(authorMembership.Id),
            Permission(otherMembership.Id));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static OperatorMembershipRecord Membership(
        string operatorId,
        string accountId,
        DateTimeOffset now) => new()
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
