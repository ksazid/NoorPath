using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorPath.FamilyBooking;
using NoorPath.FamilyBooking.Infrastructure;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Traveller.Infrastructure;
using Xunit;

namespace NoorPath.FamilyBooking.Integration.Tests;

public sealed class FamilyBookingApiTests
{
    private const string Customer = "family-customer";
    private const string OtherCustomer = "family-other-customer";

    [Fact]
    public async Task Party_mutations_are_account_scoped_and_reject_stale_versions()
    {
        using var app = await FamilyBookingApi.CreateAsync(TestContext.Current.CancellationToken);
        var (firstTraveller, secondTraveller) = await app.SeedTravellersAsync(Customer, TestContext.Current.CancellationToken);
        using var customer = app.CreateCustomer(Customer);
        using var other = app.CreateCustomer(OtherCustomer);

        var created = await customer.PostAsJsonAsync(
            "/api/v1/family-parties",
            new { name = "Khan family" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(
            await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var partyId = createdBody.RootElement.GetProperty("id").GetGuid();

        var added = await customer.PostAsJsonAsync(
            $"/api/v1/family-parties/{partyId}/members",
            new { travellerId = firstTraveller, version = 0 },
            TestContext.Current.CancellationToken);
        added.EnsureSuccessStatusCode();

        var hidden = await other.GetAsync(
            $"/api/v1/family-parties/{partyId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var stale = await customer.PostAsJsonAsync(
            $"/api/v1/family-parties/{partyId}/members",
            new { travellerId = secondTraveller, version = 0 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var staleBody = JsonDocument.Parse(
            await stale.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("stale_family_party", staleBody.RootElement.GetProperty("code").GetString());
        Assert.Equal(1, staleBody.RootElement.GetProperty("currentVersion").GetInt32());
    }

    [Fact]
    public async Task Validated_party_can_snapshot_only_an_exact_account_owned_quote()
    {
        using var app = await FamilyBookingApi.CreateAsync(TestContext.Current.CancellationToken);
        var seed = await app.SeedValidatedQuoteAsync(Customer, TestContext.Current.CancellationToken);
        using var customer = app.CreateCustomer(Customer);
        using var other = app.CreateCustomer(OtherCustomer);

        var bound = await customer.PostAsJsonAsync(
            $"/api/v1/family-parties/{seed.PartyId}/quotes/{seed.QuoteId}/snapshot",
            new { version = seed.PartyVersion },
            TestContext.Current.CancellationToken);
        bound.EnsureSuccessStatusCode();

        using (var scope = app.Services.CreateScope())
        {
            var family = scope.ServiceProvider.GetRequiredService<FamilyBookingDbContext>();
            var snapshot = await family.QuoteSnapshots.AsNoTracking().SingleAsync(
                item => item.QuoteId == seed.QuoteId,
                TestContext.Current.CancellationToken);
            Assert.Equal(seed.PartyId, snapshot.FamilyPartyId);
            Assert.Equal(seed.PartyVersion, snapshot.PartyVersion);
            Assert.Equal(FamilyBookingPolicy.CurrentVersion, snapshot.PolicyVersion);
            Assert.Contains(seed.FirstTravellerId.ToString(), snapshot.PayloadJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(seed.SecondTravellerId.ToString(), snapshot.PayloadJson, StringComparison.OrdinalIgnoreCase);
        }

        var hidden = await other.PostAsJsonAsync(
            $"/api/v1/family-parties/{seed.PartyId}/quotes/{seed.QuoteId}/snapshot",
            new { version = seed.PartyVersion },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task Quote_snapshot_rejects_a_party_with_different_travellers()
    {
        using var app = await FamilyBookingApi.CreateAsync(TestContext.Current.CancellationToken);
        var seed = await app.SeedValidatedQuoteAsync(Customer, TestContext.Current.CancellationToken);
        using var customer = app.CreateCustomer(Customer);

        using (var scope = app.Services.CreateScope())
        {
            var family = scope.ServiceProvider.GetRequiredService<FamilyBookingDbContext>();
            var member = await family.Members.SingleAsync(
                item => item.FamilyPartyId == seed.PartyId && item.TravellerId == seed.SecondTravellerId,
                TestContext.Current.CancellationToken);
            member.RemovedAtUtc = DateTimeOffset.UtcNow;
            await family.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await customer.PostAsJsonAsync(
            $"/api/v1/family-parties/{seed.PartyId}/quotes/{seed.QuoteId}/snapshot",
            new { version = seed.PartyVersion },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("family_party_quote_mismatch", body.RootElement.GetProperty("code").GetString());
    }
}

public sealed class FamilyBookingApi : WebApplicationFactory<Program>
{
    private readonly string connection;

    private FamilyBookingApi(string connection) => this.connection = connection;

    public static async Task<FamilyBookingApi> CreateAsync(CancellationToken cancellationToken)
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_FAMILY_BOOKING_TEST_DB",
            "Family Booking API");
        var app = new FamilyBookingApi(connection);
        using var scope = app.Services.CreateScope();
        var family = scope.ServiceProvider.GetRequiredService<FamilyBookingDbContext>();
        var travellers = scope.ServiceProvider.GetRequiredService<TravellerDbContext>();
        var pricing = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        await family.Database.EnsureDeletedAsync(cancellationToken);
        await family.Database.MigrateAsync(cancellationToken);
        await travellers.Database.MigrateAsync(cancellationToken);
        await pricing.Database.MigrateAsync(cancellationToken);
        return app;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestSettings.ConfigureTestHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FamilyBookingDbContext>>();
            services.RemoveAll<FamilyBookingDbContext>();
            services.RemoveAll<DbContextOptions<TravellerDbContext>>();
            services.RemoveAll<TravellerDbContext>();
            services.RemoveAll<DbContextOptions<PricingDbContext>>();
            services.RemoveAll<PricingDbContext>();

            services.AddDbContext<FamilyBookingDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(FamilyBookingDbContext).Assembly.FullName)));
            services.AddDbContext<TravellerDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(TravellerDbContext).Assembly.FullName)));
            services.AddDbContext<PricingDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    postgres => postgres.MigrationsAssembly(typeof(PricingDbContext).Assembly.FullName)));
        });
    }

    public HttpClient CreateCustomer(string identity)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-NoorPath-Test-Identity", identity);
        return client;
    }

    public async Task<(Guid First, Guid Second)> SeedTravellersAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravellerDbContext>();
        var now = DateTimeOffset.UtcNow;
        var first = new TravellerRecord
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = accountId,
            FullName = "Amina Khan",
            DateOfBirth = new DateOnly(1995, 4, 12),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var second = new TravellerRecord
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = accountId,
            FullName = "Omar Khan",
            DateOfBirth = new DateOnly(1992, 8, 20),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.AddRange(first, second);
        await db.SaveChangesAsync(cancellationToken);
        return (first.Id, second.Id);
    }

    public async Task<ValidatedQuoteSeed> SeedValidatedQuoteAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        var (first, second) = await SeedTravellersAsync(accountId, cancellationToken);
        using var scope = Services.CreateScope();
        var family = scope.ServiceProvider.GetRequiredService<FamilyBookingDbContext>();
        var pricing = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var now = DateTimeOffset.UtcNow;
        var departureId = Guid.NewGuid();
        var plan = new PricePlanRecord
        {
            Id = Guid.NewGuid(),
            DepartureId = departureId,
            OperatorId = "operator-noor",
            Currency = "INR",
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var version = new PriceVersionRecord
        {
            Id = Guid.NewGuid(),
            PricePlanId = plan.Id,
            DepartureId = departureId,
            OperatorId = plan.OperatorId,
            SourcePlanVersion = 1,
            Currency = plan.Currency,
            PublishedByAccountId = "platform-publisher",
            PublishedAtUtc = now
        };
        var quote = new QuoteRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            DepartureId = departureId,
            OperatorId = plan.OperatorId,
            PriceVersionId = version.Id,
            Occupancy = PricingOccupancy.Double,
            TravellerCount = 2,
            Currency = "INR",
            UnitPrice = 100000m,
            Total = 200000m,
            DueNow = 40000m,
            Remaining = 160000m,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(30)
        };
        pricing.PricePlans.Add(plan);
        pricing.PriceVersions.Add(version);
        pricing.Quotes.Add(quote);
        pricing.QuoteTravellers.AddRange(
            new QuoteTravellerRecord { Id = Guid.NewGuid(), QuoteId = quote.Id, TravellerId = first, Position = 1 },
            new QuoteTravellerRecord { Id = Guid.NewGuid(), QuoteId = quote.Id, TravellerId = second, Position = 2 });
        await pricing.SaveChangesAsync(cancellationToken);

        var party = new FamilyPartyRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Khan family",
            Status = FamilyPartyStatus.Validated,
            PolicyVersion = FamilyBookingPolicy.CurrentVersion,
            Version = 3,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        family.Parties.Add(party);
        family.Members.AddRange(
            new FamilyPartyMemberRecord { FamilyPartyId = party.Id, AccountId = accountId, TravellerId = first, Version = 0, AddedAtUtc = now },
            new FamilyPartyMemberRecord { FamilyPartyId = party.Id, AccountId = accountId, TravellerId = second, Version = 0, AddedAtUtc = now });
        family.MahramLinks.Add(new MahramLinkRecord
        {
            Id = Guid.NewGuid(),
            FamilyPartyId = party.Id,
            AccountId = accountId,
            ProtectedTravellerId = first,
            MahramTravellerId = second,
            RelationshipType = MahramRelationshipType.Brother,
            Declaration = "I confirm this family relationship is accurate.",
            IsActive = true,
            Version = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await family.SaveChangesAsync(cancellationToken);
        return new(party.Id, party.Version, quote.Id, first, second);
    }
}

public sealed record ValidatedQuoteSeed(
    Guid PartyId,
    int PartyVersion,
    Guid QuoteId,
    Guid FirstTravellerId,
    Guid SecondTravellerId);
