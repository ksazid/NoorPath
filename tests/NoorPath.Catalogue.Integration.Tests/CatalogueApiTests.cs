using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Testing;
using Xunit;

namespace NoorPath.Catalogue.Integration.Tests;

public sealed class CatalogueApiTests
{
    [Fact]
    public async Task Authenticated_admin_can_create_publish_and_discover_a_batch()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var draft = await CreateDraft(admin, BatchCommand());
        var publish = await admin.PostAsJsonAsync($"/api/v1/admin/batches/{draft.Id}/publish", new { expectedVersion = draft.Version, operatorId = "test-approved-noor" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        using var customer = app.CreateClient();
        var json = await customer.GetStringAsync("/api/v1/batches", TestContext.Current.CancellationToken);
        Assert.Contains(draft.Id.ToString(), json);
        Assert.Contains("Noor Comfort", json);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Draft_creation_denies_missing_or_invalid_credentials(bool includeInvalidCredential)
    {
        using var app = await CatalogueApi.CreateAsync();
        using var client = app.CreateClient();
        if (includeInvalidCredential) client.DefaultRequestHeaders.Add("X-NoorPath-Admin", "not-authorised");
        var response = await client.PostAsJsonAsync("/api/v1/admin/batches", BatchCommand(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("Noor Comfort", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Publication_denies_missing_credentials()
    {
        using var app = await CatalogueApi.CreateAsync();
        var draft = await CreateDraft(app.CreateAdminClient(), BatchCommand());
        using var customer = app.CreateClient();
        var response = await customer.PostAsJsonAsync($"/api/v1/admin/batches/{draft.Id}/publish", new { expectedVersion = draft.Version, operatorId = "test-approved-noor" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Publication_hides_cross_operator_batches()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var draft = await CreateDraft(admin, BatchCommand());
        var response = await admin.PostAsJsonAsync($"/api/v1/admin/batches/{draft.Id}/publish", new { expectedVersion = draft.Version, operatorId = "test-approved-rahma" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertDraftHasNoAudit(app, draft.Id);
    }

    [Fact]
    public async Task Unapproved_operator_is_rejected_without_persistence()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var response = await admin.PostAsJsonAsync("/api/v1/admin/batches", BatchCommand() with { OperatorId = "live-unapproved" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        Assert.Empty(await db.Batches.ToListAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Invalid_input_returns_rfc9457_validation_problem()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var invalid = BatchCommand() with { PackageName = "", Capacity = 0, ReturnDate = new(2026, 10, 9), TotalPriceInr = 0 };
        var response = await admin.PostAsJsonAsync("/api/v1/admin/batches", invalid, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(422, problem.RootElement.GetProperty("status").GetInt32());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("packageName", out _));
        Assert.True(errors.TryGetProperty("capacity", out _));
        Assert.True(errors.TryGetProperty("returnDate", out _));
        Assert.True(errors.TryGetProperty("totalPriceInr", out _));
    }

    [Fact]
    public async Task Stale_publication_rolls_back_status_and_audit()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var draft = await CreateDraft(admin, BatchCommand());
        var response = await admin.PostAsJsonAsync($"/api/v1/admin/batches/{draft.Id}/publish", new { expectedVersion = draft.Version + 1, operatorId = "test-approved-noor" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertDraftHasNoAudit(app, draft.Id);
    }

    [Fact]
    public async Task Duplicate_publication_is_a_conflict_and_does_not_duplicate_audit()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var draft = await CreateDraft(admin, BatchCommand());
        var request = new { expectedVersion = draft.Version, operatorId = "test-approved-noor" };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/batches/{draft.Id}/publish", request, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/batches/{draft.Id}/publish", request, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        Assert.Single(await db.PublicationAudits.Where(x => x.BatchId == draft.Id).ToListAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Public_projection_excludes_drafts_and_private_fields_and_is_cacheable()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var admin = app.CreateAdminClient();
        var draft = await CreateDraft(admin, BatchCommand() with { PackageName = "Never public draft" });
        var published = await CreateDraft(admin, BatchCommand() with { PackageName = "Visible journey" });
        await admin.PostAsJsonAsync($"/api/v1/admin/batches/{published.Id}/publish", new { expectedVersion = published.Version, operatorId = "test-approved-noor" }, cancellationToken: TestContext.Current.CancellationToken);

        using var customer = app.CreateClient();
        var response = await customer.GetAsync("/api/v1/batches", TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("max-age=60", response.Headers.CacheControl?.ToString());
        Assert.Contains("Visible journey", json);
        Assert.DoesNotContain("Never public draft", json);
        Assert.DoesNotContain("operatorId", json);
        Assert.DoesNotContain("publishedAt", json);
        Assert.DoesNotContain("actor", json);
        Assert.DoesNotContain("correlationId", json);
        Assert.DoesNotContain("version", json);
    }

    [Fact]
    public async Task Public_catalogue_rate_limit_returns_too_many_requests()
    {
        using var app = await CatalogueApi.CreateAsync();
        using var customer = app.CreateClient();
        for (var request = 0; request < 60; request++) Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/batches", TestContext.Current.CancellationToken)).StatusCode);
        var response = await customer.GetAsync("/api/v1/batches", TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable, $"Expected throttling, got {(int)response.StatusCode} {response.StatusCode}");
    }

    private static async Task<DraftResponse> CreateDraft(HttpClient client, CreateBatch command)
    {
        var response = await client.PostAsJsonAsync("/api/v1/admin/batches", command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DraftResponse>())!;
    }

    private static CreateBatch BatchCommand() => new("test-approved-noor", "Noor Tours", "Noor Comfort", "A supported journey", "Comfort", "Delhi", "Jeddah to Makkah", new(2026, 10, 10), new(2026, 10, 22), 24, AvailabilityMode.Exact, 94500, ["Flights", "Breakfast"]);

    private static async Task AssertDraftHasNoAudit(CatalogueApi app, Guid batchId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        Assert.Equal(BatchStatus.Draft, (await db.Batches.SingleAsync(x => x.Id == batchId)).Status);
        Assert.Empty(await db.PublicationAudits.Where(x => x.BatchId == batchId).ToListAsync());
    }

    private sealed record DraftResponse(Guid Id, int Version, string Status);
}

public sealed class CatalogueApi : WebApplicationFactory<Program>
{
    private readonly string connection;
    private CatalogueApi(string connection) => this.connection = connection;

    public static async Task<CatalogueApi> CreateAsync()
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_CATALOGUE_TEST_DB",
            "Catalogue API");

        var app = new CatalogueApi(connection);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        return app;
    }

    //Integration tests explicitly override the production DbContext registration.

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestSettings.ConfigureTestHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogueDbContext>>();
            services.RemoveAll<CatalogueDbContext>();

            services.AddDbContext<CatalogueDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(
                        typeof(CatalogueDbContext).Assembly.FullName)));
        });
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-NoorPath-Admin", "s02-pilot-admin");
        return client;
    }
}
