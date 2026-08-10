using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Inventory.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class PackageDetailsConversionApiTests
{
    [Fact]
    public async Task Public_package_details_include_same_origin_dates_and_authoritative_payment_preview()
    {
        using var app = await CommercialApi.CreateAsync(TestContext.Current.CancellationToken);
        var currentDeparture = await PublishDepartureAsync(
            app,
            new DateOnly(2028, 10, 10),
            TestContext.Current.CancellationToken);
        var soldOutDeparture = await PublishDepartureAsync(
            app,
            new DateOnly(2028, 10, 24),
            TestContext.Current.CancellationToken);

        using (var scope = app.Services.CreateScope())
        {
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var configuration = await inventory.Configurations.SingleAsync(
                item => item.DepartureId == soldOutDeparture,
                TestContext.Current.CancellationToken);
            var pools = await inventory.Pools
                .Where(item => item.InventoryConfigurationId == configuration.Id)
                .ToListAsync(TestContext.Current.CancellationToken);
            foreach (var pool in pools)
                pool.Capacity = 0;
            await inventory.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var anonymous = app.CreateClient();
        var response = await anonymous.GetAsync(
            $"/api/v1/departures/{currentDeparture}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;
        var dates = root.GetProperty("travelDates").EnumerateArray().ToArray();
        Assert.Equal(2, dates.Length);
        Assert.Equal(currentDeparture, dates[0].GetProperty("departureId").GetGuid());
        Assert.Equal("available", dates[0].GetProperty("status").GetString());
        Assert.Equal(soldOutDeparture, dates[1].GetProperty("departureId").GetGuid());
        Assert.Equal("sold-out", dates[1].GetProperty("status").GetString());

        var doubleRoom = root
            .GetProperty("pricing")
            .GetProperty("occupancies")
            .EnumerateArray()
            .Single(item => item.GetProperty("occupancy").GetString() == "double");
        var financials = doubleRoom.GetProperty("financials");
        Assert.Equal(2, financials.GetProperty("adultGuests").GetInt32());
        Assert.Equal(220000m, financials.GetProperty("total").GetDecimal());
        Assert.Equal(44000m, financials.GetProperty("dueNow").GetDecimal());
        Assert.Equal(176000m, financials.GetProperty("remaining").GetDecimal());
        Assert.NotEmpty(financials.GetProperty("instalments").EnumerateArray());
        Assert.NotEqual(JsonValueKind.Null, financials.GetProperty("finalDueDate").ValueKind);
    }

    private static async Task<Guid> PublishDepartureAsync(
        CommercialApi app,
        DateOnly departureDate,
        CancellationToken cancellationToken)
    {
        using var author = app.CreateOperatorClient(CommercialApi.AuthorIdentity);
        using var approver = app.CreateOperatorClient(CommercialApi.PlatformApproverIdentity);

        var create = await author.PostAsJsonAsync(
            "/api/v1/operator/departures",
            new SaveCatalogueDraftRequest(
                "Mumbai Standard 14 Nights",
                "A published Mumbai Umrah journey with authoritative customer-facing facts.",
                new(
                    "Pullman ZamZam Makkah",
                    "5 star",
                    "450 m from Masjid al-Haram",
                    7,
                    "confirmed"),
                new(
                    "Anwar Al Madinah Mövenpick",
                    "5 star",
                    "200 m from Al-Masjid an-Nabawi",
                    7,
                    "pending"),
                new(
                    "Mumbai → Jeddah → Makkah → Madinah",
                    "Intercity transfer by bus. Final flight timing remains pending.",
                    "pending"),
                "Mumbai (BOM)",
                departureDate,
                departureDate.AddDays(14),
                [
                    "Return flights",
                    "Visa included",
                    "Makkah accommodation",
                    "Madinah accommodation",
                    "Breakfast, lunch and dinner",
                    "Intercity travel by bus",
                    "Luggage tag",
                    "Pocket Dua guide"
                ],
                ["Personal expenses"]),
            cancellationToken);
        create.EnsureSuccessStatusCode();
        using var createBody = JsonDocument.Parse(
            await create.Content.ReadAsStringAsync(cancellationToken));
        var departureId = createBody.RootElement.GetProperty("departureId").GetGuid();

        var pricing = await author.PutAsJsonAsync(
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
        pricing.EnsureSuccessStatusCode();
        using var pricingBody = JsonDocument.Parse(
            await pricing.Content.ReadAsStringAsync(cancellationToken));
        var pricingVersion = pricingBody.RootElement.GetProperty("version").GetInt32();

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/payment-plan",
            new SavePaymentPlanRequest(pricingVersion, true, 20m, 10, 30),
            cancellationToken)).EnsureSuccessStatusCode();

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/inventory",
            new SaveInventoryRequest(
                0,
                "Initial customer package allocation",
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

    private sealed record PublicationReviewResponse(
        string Status,
        bool Ready,
        int DepartureVersion,
        int PricingVersion,
        int InventoryVersion);
}