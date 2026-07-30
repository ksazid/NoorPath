using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NoorPath.Commercial.Integration.Tests;

public sealed class CustomerDiscoveryApiTests
{
    [Fact]
    public async Task Anonymous_customer_sees_published_departure_with_snapshot_price_and_availability()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(app, TestContext.Current.CancellationToken);
        using var anonymous = app.CreateClient();

        var response = await anonymous.GetAsync(
            "/api/v1/departures",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(departureId, item.GetProperty("departureId").GetGuid());
        Assert.Equal(
            "Noor International Tours & Travels",
            item.GetProperty("operator").GetProperty("displayName").GetString());
        Assert.Equal("Noor Discovery 12 Nights", item.GetProperty("packageName").GetString());
        Assert.Equal("INR", item.GetProperty("headlinePrice").GetProperty("currency").GetString());
        Assert.Equal("quad", item.GetProperty("headlinePrice").GetProperty("occupancy").GetString());
        Assert.Equal(90000m, item.GetProperty("headlinePrice").GetProperty("amount").GetDecimal());
        Assert.Equal("available", item.GetProperty("availability").GetProperty("status").GetString());
        Assert.Equal(
            3,
            item.GetProperty("availability").GetProperty("occupancies").GetArrayLength());
        Assert.Equal(
            ["Return flights", "Breakfast"],
            item.GetProperty("inclusionHighlights")
                .EnumerateArray()
                .Select(x => x.GetString()!)
                .ToArray());
    }

    [Fact]
    public async Task Draft_and_ready_for_review_departures_are_not_public()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        using var author = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        using var anonymous = app.CreateClient();
        var departureId = await CreateDraftAsync(author, TestContext.Current.CancellationToken);

        await AssertNoPublicItemsAsync(anonymous, TestContext.Current.CancellationToken);

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            PublishableInventory(),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var review = await author.GetFromJsonAsync<PublicationReviewResponse>(
            $"/api/v1/operator/departures/{departureId}/publication-review",
            TestContext.Current.CancellationToken);
        Assert.NotNull(review);

        (await author.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/submit-review",
            new PublicationVersionRequest(
                review.DepartureVersion,
                review.PricingVersion,
                review.InventoryVersion),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        await AssertNoPublicItemsAsync(anonymous, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Published_departure_is_hidden_when_operator_is_suspended()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        await PublishDepartureAsync(app, TestContext.Current.CancellationToken);

        using (var scope = app.Services.CreateScope())
        {
            var operators = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
            var operation = await operators.Operators.SingleAsync(
                x => x.Id == "operator-noor",
                TestContext.Current.CancellationToken);
            operation.State = OperatorState.Suspended;
            operation.Version++;
            operation.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await operators.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var anonymous = app.CreateClient();
        await AssertNoPublicItemsAsync(anonymous, TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> PublishDepartureAsync(
        CommercialApi app,
        CancellationToken cancellationToken)
    {
        using var author = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        using var approver = app.CreateOperatorClient(CommercialApi.PlatformApproverIdentity);
        var departureId = await CreateDraftAsync(author, cancellationToken);

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            ValidPricing(),
            cancellationToken)).EnsureSuccessStatusCode();
        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            PublishableInventory(),
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

    private static async Task<Guid> CreateDraftAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/operator/departures",
            new SaveCatalogueDraftRequest(
                "Noor Discovery 12 Nights",
                "A published journey prepared for customer discovery.",
                new("Makkah Hotel", "4 star", "850 m from Masjid al-Haram", 6, "confirmed"),
                new("Madinah Hotel", "4 star", "450 m from Al-Masjid an-Nabawi", 5, "confirmed"),
                new("Delhi → Jeddah → Makkah → Madinah", "Final flight facts remain pending.", "pending"),
                "Delhi (DEL)",
                new(2026, 10, 10),
                new(2026, 10, 22),
                ["Return flights", "Breakfast"],
                ["Personal expenses"]),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<DraftResponse>(
            cancellationToken: cancellationToken))!;
        return created.DepartureId;
    }

    private static SavePricingRequest ValidPricing() => new(
        0,
        "INR",
        [
            new("double", 110000m),
            new("triple", 100000m),
            new("quad", 90000m)
        ]);

    private static SaveInventoryRequest PublishableInventory() => new(
        0,
        "Initial publication allocation",
        [
            new("double", 10),
            new("triple", 8),
            new("quad", 6)
        ]);

    private static async Task AssertNoPublicItemsAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync("/api/v1/departures", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        Assert.Empty(body.RootElement.GetProperty("items").EnumerateArray());
    }

    private sealed record PublicationReviewResponse(
        string Status,
        bool Ready,
        int DepartureVersion,
        int PricingVersion,
        int InventoryVersion);

    private sealed record DraftResponse(Guid DepartureId);
}
