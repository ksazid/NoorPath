using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Inventory.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class QuoteApiTests
{
    private const string CustomerIdentity = "quote-customer";
    private const string OtherCustomerIdentity = "quote-other-customer";

    [Fact]
    public async Task Authenticated_customer_receives_authoritative_payment_plan_quote_without_reserving_inventory()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            withPaymentPlan: true,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient(CustomerIdentity);
        var travellerIds = await CreateAdultTravellersAsync(
            customer,
            2,
            TestContext.Current.CancellationToken);

        var capacityBefore = await GetDoubleCapacityAsync(
            app,
            departureId,
            TestContext.Current.CancellationToken);

        var response = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId}/quotes",
            new CreateQuoteRequest("double", travellerIds),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;
        Assert.Equal(departureId, root.GetProperty("departureId").GetGuid());
        Assert.Equal("double", root.GetProperty("occupancy").GetString());
        Assert.Equal(2, root.GetProperty("travellerCount").GetInt32());
        Assert.Equal("INR", root.GetProperty("currency").GetString());
        Assert.Equal(110000m, root.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(220000m, root.GetProperty("total").GetDecimal());
        Assert.Equal(44000m, root.GetProperty("dueNow").GetDecimal());
        Assert.Equal(176000m, root.GetProperty("remaining").GetDecimal());
        Assert.False(root.GetProperty("availabilityReserved").GetBoolean());
        Assert.False(root.GetProperty("expired").GetBoolean());

        var instalments = root.GetProperty("instalments").EnumerateArray().ToArray();
        Assert.NotEmpty(instalments);
        Assert.Equal(
            176000m,
            instalments.Sum(item => item.GetProperty("amount").GetDecimal()));
        Assert.Equal(
            220000m,
            root.GetProperty("dueNow").GetDecimal() +
            instalments.Sum(item => item.GetProperty("amount").GetDecimal()));

        var createdAt = root.GetProperty("createdAtUtc").GetDateTimeOffset();
        var expiresAt = root.GetProperty("expiresAtUtc").GetDateTimeOffset();
        Assert.Equal(TimeSpan.FromMinutes(30), expiresAt - createdAt);

        var capacityAfter = await GetDoubleCapacityAsync(
            app,
            departureId,
            TestContext.Current.CancellationToken);
        Assert.Equal(capacityBefore, capacityAfter);
    }

    [Fact]
    public async Task Published_price_without_payment_plan_requires_full_amount_now()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            withPaymentPlan: false,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient(CustomerIdentity);
        var travellerIds = await CreateAdultTravellersAsync(
            customer,
            2,
            TestContext.Current.CancellationToken);

        var response = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId}/quotes",
            new CreateQuoteRequest("double", travellerIds),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;
        Assert.Equal(220000m, root.GetProperty("total").GetDecimal());
        Assert.Equal(220000m, root.GetProperty("dueNow").GetDecimal());
        Assert.Equal(0m, root.GetProperty("remaining").GetDecimal());
        Assert.Empty(root.GetProperty("instalments").EnumerateArray());
    }

    [Fact]
    public async Task Traveller_and_quote_endpoints_are_account_scoped()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            withPaymentPlan: true,
            TestContext.Current.CancellationToken);
        using var anonymous = app.CreateClient();
        using var customer = app.CreateOperatorClient(CustomerIdentity);
        using var otherCustomer = app.CreateOperatorClient(OtherCustomerIdentity);

        var unauthenticated = await anonymous.GetAsync(
            "/api/v1/travellers",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        var travellerIds = await CreateAdultTravellersAsync(
            customer,
            2,
            TestContext.Current.CancellationToken);
        var quote = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId}/quotes",
            new CreateQuoteRequest("double", travellerIds),
            TestContext.Current.CancellationToken);
        quote.EnsureSuccessStatusCode();
        using var quoteBody = JsonDocument.Parse(
            await quote.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var quoteId = quoteBody.RootElement.GetProperty("quoteId").GetGuid();

        var hidden = await otherCustomer.GetAsync(
            $"/api/v1/quotes/{quoteId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task Quote_rejects_underage_or_wrong_room_party_without_creating_commitment()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var departureId = await PublishDepartureAsync(
            app,
            withPaymentPlan: true,
            TestContext.Current.CancellationToken);
        using var customer = app.CreateOperatorClient(CustomerIdentity);

        var adult = await CreateTravellerAsync(
            customer,
            "Adult Traveller",
            new DateOnly(1990, 1, 1),
            TestContext.Current.CancellationToken);
        var child = await CreateTravellerAsync(
            customer,
            "Young Traveller",
            new DateOnly(2015, 1, 1),
            TestContext.Current.CancellationToken);

        var underage = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId}/quotes",
            new CreateQuoteRequest("double", [adult, child]),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, underage.StatusCode);

        var wrongCount = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId}/quotes",
            new CreateQuoteRequest("triple", [adult, child]),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongCount.StatusCode);
    }

    private static async Task<Guid> PublishDepartureAsync(
        CommercialApi app,
        bool withPaymentPlan,
        CancellationToken cancellationToken)
    {
        using var author = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        using var approver = app.CreateOperatorClient(CommercialApi.PlatformApproverIdentity);
        var departureId = await CreateDraftAsync(author, cancellationToken);

        var pricingResponse = await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/pricing",
            new SavePricingRequest(
                0,
                "INR",
                [
                    new("double", 110000m),
                    new("triple", 100000m),
                    new("quad", 90000m)
                ]),
            cancellationToken);
        pricingResponse.EnsureSuccessStatusCode();

        if (withPaymentPlan)
        {
            var plan = await author.PutAsJsonAsync(
                $"/api/v1/operator/departures/{departureId}/payment-plan",
                new SavePaymentPlanRequest(
                    ExpectedPricingVersion: 1,
                    Enabled: true,
                    DepositPercent: 20m,
                    InstalmentDayOfMonth: 5,
                    FinalPaymentDueDaysBeforeDeparture: 30),
                cancellationToken);
            plan.EnsureSuccessStatusCode();
        }

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            new SaveInventoryRequest(
                0,
                "Initial quote-test allocation",
                [
                    new("double", 10),
                    new("triple", 8),
                    new("quad", 6)
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

    private static async Task<Guid> CreateDraftAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var departureDate = new DateOnly(2027, 8, 14);
        var response = await client.PostAsJsonAsync(
            "/api/v1/operator/departures",
            new SaveCatalogueDraftRequest(
                "Plan Ahead August Umrah",
                "Future departure for authoritative quote verification.",
                new("Makkah Hotel", "4 star", "850 m from Masjid al-Haram", 7, "confirmed"),
                new("Madinah Hotel", "4 star", "450 m from Al-Masjid an-Nabawi", 5, "confirmed"),
                new("Delhi → Jeddah → Makkah → Madinah", "Published routing.", "confirmed"),
                "Delhi (DEL)",
                departureDate,
                departureDate.AddDays(12),
                ["Return flights", "Breakfast", "Journey support"],
                ["Personal expenses"]),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        return body.RootElement.GetProperty("departureId").GetGuid();
    }

    private static async Task<Guid[]> CreateAdultTravellersAsync(
        HttpClient customer,
        int count,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        for (var index = 0; index < count; index++)
        {
            ids.Add(await CreateTravellerAsync(
                customer,
                $"Traveller {index + 1}",
                new DateOnly(1990 + index, 1, 1),
                cancellationToken));
        }

        return ids.ToArray();
    }

    private static async Task<Guid> CreateTravellerAsync(
        HttpClient customer,
        string fullName,
        DateOnly dateOfBirth,
        CancellationToken cancellationToken)
    {
        var response = await customer.PostAsJsonAsync(
            "/api/v1/travellers",
            new CreateTravellerRequest(fullName, dateOfBirth.ToString("yyyy-MM-dd")),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        return body.RootElement.GetProperty("travellerId").GetGuid();
    }

    private static async Task<int> GetDoubleCapacityAsync(
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
                item.Occupancy.ToString() == "Double")
            .Select(item => item.Capacity)
            .SingleAsync(cancellationToken);
    }

    private sealed record PublicationReviewResponse(
        string Status,
        bool Ready,
        int DepartureVersion,
        int PricingVersion,
        int InventoryVersion);
}
