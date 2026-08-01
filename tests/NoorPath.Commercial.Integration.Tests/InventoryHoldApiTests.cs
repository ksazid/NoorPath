using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class InventoryHoldApiTests
{
    [Fact]
    public async Task Hold_requires_authentication_and_idempotency_key()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            doubleCapacity: 2,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient("hold-required-customer");
        using var anonymous = app.CreateClient();
        var quoteId = await CreateDoubleQuoteAsync(
            customer,
            departureId,
            TestContext.Current.CancellationToken);

        var unauthenticated = await SendHoldAsync(
            anonymous,
            quoteId,
            "hold-required-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        var missingKey = await customer.PostAsync(
            $"/api/v1/quotes/{quoteId}/holds",
            content: null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        Assert.Equal(
            "idempotency_key_required",
            await ReadProblemCodeAsync(missingKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Owner_can_acquire_replay_read_and_release_one_hold_without_changing_capacity()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            doubleCapacity: 2,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient("hold-owner-customer");
        using var other = app.CreateOperatorClient("hold-other-customer");
        var quoteId = await CreateDoubleQuoteAsync(
            customer,
            departureId,
            TestContext.Current.CancellationToken);

        var created = await SendHoldAsync(
            customer,
            quoteId,
            "hold-owner-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await ReadJsonAsync(created, TestContext.Current.CancellationToken);
        var holdId = createdBody.RootElement.GetProperty("holdId").GetGuid();
        Assert.Equal("active", createdBody.RootElement.GetProperty("status").GetString());
        Assert.True(createdBody.RootElement.GetProperty("availabilityReserved").GetBoolean());
        Assert.Equal(1, createdBody.RootElement.GetProperty("quantity").GetInt32());
        var createdAt = createdBody.RootElement.GetProperty("createdAtUtc").GetDateTimeOffset();
        var expiresAt = createdBody.RootElement.GetProperty("expiresAtUtc").GetDateTimeOffset();
        Assert.InRange(expiresAt - createdAt, TimeSpan.Zero, TimeSpan.FromMinutes(15));

        var replay = await SendHoldAsync(
            customer,
            quoteId,
            "hold-owner-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var replayBody = await ReadJsonAsync(replay, TestContext.Current.CancellationToken);
        Assert.Equal(holdId, replayBody.RootElement.GetProperty("holdId").GetGuid());

        var hidden = await other.GetAsync(
            $"/api/v1/inventory-holds/{holdId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        Assert.Equal(
            1,
            await GetDoubleAvailableQuantityAsync(
                customer,
                departureId,
                TestContext.Current.CancellationToken));
        Assert.Equal(
            2,
            await GetConfiguredDoubleCapacityAsync(
                app,
                departureId,
                TestContext.Current.CancellationToken));

        var released = await customer.PostAsync(
            $"/api/v1/inventory-holds/{holdId}/release",
            content: null,
            TestContext.Current.CancellationToken);
        released.EnsureSuccessStatusCode();
        var releasedBody = await ReadJsonAsync(released, TestContext.Current.CancellationToken);
        Assert.Equal("released", releasedBody.RootElement.GetProperty("status").GetString());
        Assert.False(releasedBody.RootElement.GetProperty("availabilityReserved").GetBoolean());

        var replayedRelease = await customer.PostAsync(
            $"/api/v1/inventory-holds/{holdId}/release",
            content: null,
            TestContext.Current.CancellationToken);
        replayedRelease.EnsureSuccessStatusCode();
        Assert.Equal(
            2,
            await GetDoubleAvailableQuantityAsync(
                customer,
                departureId,
                TestContext.Current.CancellationToken));

        using var scope = app.Services.CreateScope();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var hold = await inventory.Holds.AsNoTracking()
            .SingleAsync(item => item.Id == holdId, TestContext.Current.CancellationToken);
        Assert.Equal(InventoryHoldState.Released, hold.State);
        Assert.NotNull(hold.TerminalAtUtc);
        Assert.Single(await inventory.Holds.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Reusing_an_idempotency_key_for_another_quote_is_rejected()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            doubleCapacity: 2,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient("hold-conflict-customer");
        var firstQuote = await CreateDoubleQuoteAsync(
            customer,
            departureId,
            TestContext.Current.CancellationToken);
        var secondQuote = await CreateDoubleQuoteAsync(
            customer,
            departureId,
            TestContext.Current.CancellationToken);

        var first = await SendHoldAsync(
            customer,
            firstQuote,
            "hold-conflict-0001",
            TestContext.Current.CancellationToken);
        first.EnsureSuccessStatusCode();

        var conflict = await SendHoldAsync(
            customer,
            secondQuote,
            "hold-conflict-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(
            "idempotency_conflict",
            await ReadProblemCodeAsync(conflict, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Capacity_one_allows_exactly_one_hold_under_concurrent_customers()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            doubleCapacity: 1,
            TestContext.Current.CancellationToken);

        var attempts = new List<(HttpClient Client, Guid QuoteId, string Key)>();
        for (var index = 0; index < 6; index++)
        {
            var client = app.CreateOperatorClient($"hold-race-customer-{index}");
            var quoteId = await CreateDoubleQuoteAsync(
                client,
                departureId,
                TestContext.Current.CancellationToken);
            attempts.Add((client, quoteId, $"hold-race-{index:D4}"));
        }

        try
        {
            var responses = await Task.WhenAll(attempts.Select(attempt =>
                SendHoldAsync(
                    attempt.Client,
                    attempt.QuoteId,
                    attempt.Key,
                    TestContext.Current.CancellationToken)));

            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(5, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

            foreach (var response in responses.Where(response => response.StatusCode == HttpStatusCode.Conflict))
            {
                Assert.Equal(
                    "hold_unavailable",
                    await ReadProblemCodeAsync(response, TestContext.Current.CancellationToken));
            }

            using var scope = app.Services.CreateScope();
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            Assert.Equal(
                1,
                await inventory.Holds.CountAsync(
                    item => item.State == InventoryHoldState.Active,
                    TestContext.Current.CancellationToken));
            Assert.Equal(
                0,
                await GetDoubleAvailableQuantityAsync(
                    attempts[0].Client,
                    departureId,
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            foreach (var attempt in attempts)
                attempt.Client.Dispose();
        }
    }

    [Fact]
    public async Task Elapsed_hold_is_materialized_and_returns_capacity_on_read()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            doubleCapacity: 1,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient("hold-expiry-customer");
        var quoteId = await CreateDoubleQuoteAsync(
            customer,
            departureId,
            TestContext.Current.CancellationToken);
        var created = await SendHoldAsync(
            customer,
            quoteId,
            "hold-expiry-0001",
            TestContext.Current.CancellationToken);
        created.EnsureSuccessStatusCode();
        var createdBody = await ReadJsonAsync(created, TestContext.Current.CancellationToken);
        var holdId = createdBody.RootElement.GetProperty("holdId").GetGuid();

        using (var scope = app.Services.CreateScope())
        {
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var hold = await inventory.Holds.SingleAsync(
                item => item.Id == holdId,
                TestContext.Current.CancellationToken);
            hold.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20);
            hold.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await inventory.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var read = await customer.GetAsync(
            $"/api/v1/inventory-holds/{holdId}",
            TestContext.Current.CancellationToken);
        read.EnsureSuccessStatusCode();
        var readBody = await ReadJsonAsync(read, TestContext.Current.CancellationToken);
        Assert.Equal("expired", readBody.RootElement.GetProperty("status").GetString());
        Assert.False(readBody.RootElement.GetProperty("availabilityReserved").GetBoolean());
        Assert.Equal(
            1,
            await GetDoubleAvailableQuantityAsync(
                customer,
                departureId,
                TestContext.Current.CancellationToken));
    }

    private static async Task<HttpResponseMessage> SendHoldAsync(
        HttpClient client,
        Guid quoteId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/quotes/{quoteId}/holds");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<Guid> CreateDoubleQuoteAsync(
        HttpClient customer,
        Guid departureId,
        CancellationToken cancellationToken)
    {
        var travellerIds = new List<Guid>();
        for (var index = 0; index < 2; index++)
        {
            var response = await customer.PostAsJsonAsync(
                "/api/v1/travellers",
                new CreateTravellerRequest(
                    $"Hold Traveller {Guid.NewGuid():N}",
                    new DateOnly(1990 + index, 1, 1).ToString("yyyy-MM-dd")),
                cancellationToken);
            response.EnsureSuccessStatusCode();
            using var body = await ReadJsonAsync(response, cancellationToken);
            travellerIds.Add(body.RootElement.GetProperty("travellerId").GetGuid());
        }

        var quote = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId}/quotes",
            new CreateQuoteRequest("double", travellerIds),
            cancellationToken);
        quote.EnsureSuccessStatusCode();
        using var quoteBody = await ReadJsonAsync(quote, cancellationToken);
        return quoteBody.RootElement.GetProperty("quoteId").GetGuid();
    }

    private static async Task<Guid> PublishDepartureAsync(
        CommercialApi app,
        int doubleCapacity,
        CancellationToken cancellationToken)
    {
        using var author = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        using var approver = app.CreateOperatorClient(CommercialApi.PlatformApproverIdentity);
        var departureDate = new DateOnly(2027, 8, 14);
        var draft = await author.PostAsJsonAsync(
            "/api/v1/operator/departures",
            new SaveCatalogueDraftRequest(
                "VS-08 Inventory Hold Umrah",
                "Published departure for atomic inventory hold verification.",
                new("Makkah Hotel", "4 star", "850 m from Masjid al-Haram", 7, "confirmed"),
                new("Madinah Hotel", "4 star", "450 m from Al-Masjid an-Nabawi", 5, "confirmed"),
                new("Delhi → Jeddah → Makkah → Madinah", "Published routing.", "confirmed"),
                "Delhi (DEL)",
                departureDate,
                departureDate.AddDays(12),
                ["Return flights", "Breakfast", "Journey support"],
                ["Personal expenses"]),
            cancellationToken);
        draft.EnsureSuccessStatusCode();
        using var draftBody = await ReadJsonAsync(draft, cancellationToken);
        var departureId = draftBody.RootElement.GetProperty("departureId").GetGuid();

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            new SavePricingRequest(
                0,
                "INR",
                [
                    new("double", 110000m),
                    new("triple", 100000m),
                    new("quad", 90000m)
                ]),
            cancellationToken)).EnsureSuccessStatusCode();

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            new SaveInventoryRequest(
                0,
                "Initial VS-08 allocation",
                [
                    new("double", doubleCapacity),
                    new("triple", 2),
                    new("quad", 2)
                ]),
            cancellationToken)).EnsureSuccessStatusCode();

        var review = await author.GetFromJsonAsync<PublicationReviewResponse>(
            $"/api/v1/operator/departures/{departureId}/publication-review",
            cancellationToken);
        Assert.NotNull(review);
        (await author.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/submit-review",
            new PublicationVersionRequest(
                review.DepartureVersion,
                review.PricingVersion,
                review.InventoryVersion),
            cancellationToken)).EnsureSuccessStatusCode();

        var submitted = await approver.GetFromJsonAsync<PublicationReviewResponse>(
            $"/api/v1/platform/publications/{departureId}",
            cancellationToken);
        Assert.NotNull(submitted);
        (await approver.PostAsJsonAsync(
            $"/api/v1/platform/publications/{departureId}/publish",
            new PublicationVersionRequest(
                submitted.DepartureVersion,
                submitted.PricingVersion,
                submitted.InventoryVersion),
            cancellationToken)).EnsureSuccessStatusCode();

        return departureId;
    }

    private static async Task<int> GetDoubleAvailableQuantityAsync(
        HttpClient client,
        Guid departureId,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(
            $"/api/v1/departures/{departureId}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = await ReadJsonAsync(response, cancellationToken);
        return body.RootElement
            .GetProperty("pricing")
            .GetProperty("occupancies")
            .EnumerateArray()
            .Single(item => item.GetProperty("occupancy").GetString() == "double")
            .GetProperty("availableQuantity")
            .GetInt32();
    }

    private static async Task<int> GetConfiguredDoubleCapacityAsync(
        CommercialApi app,
        Guid departureId,
        CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var configuration = await inventory.Configurations.AsNoTracking()
            .SingleAsync(item => item.DepartureId == departureId, cancellationToken);
        return await inventory.Pools.AsNoTracking()
            .Where(item =>
                item.InventoryConfigurationId == configuration.Id &&
                item.Occupancy == InventoryOccupancy.Double)
            .Select(item => item.Capacity)
            .SingleAsync(cancellationToken);
    }

    private static async Task<string?> ReadProblemCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var body = await ReadJsonAsync(response, cancellationToken);
        return body.RootElement.GetProperty("code").GetString();
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

    private sealed record PublicationReviewResponse(
        string Status,
        bool Ready,
        int DepartureVersion,
        int PricingVersion,
        int InventoryVersion);
}
