using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Testing;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class OperatorBookingAmendmentApiTests
{
    [Fact]
    public async Task Foreign_operator_booking_preview_is_safe_not_found()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.ForeignBookingId}/amendments/preview",
            OperatorBookingAmendmentApi.DoubleProposal(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("operator-b", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NP-VS25-FOREIGN", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Confirmed_amendment_updates_booking_snapshot_appends_audit_and_leaves_payment_history_untouched()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/amendments/preview",
            OperatorBookingAmendmentApi.TripleProposal(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        using var previewBody = JsonDocument.Parse(
            await previewResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var previewToken = previewBody.RootElement.GetProperty("previewToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(previewToken));
        Assert.Equal(50_000m, previewBody.RootElement.GetProperty("priceDelta").GetDecimal());

        int paymentsBefore;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            paymentsBefore = await payments.PaymentAttempts.CountAsync(TestContext.Current.CancellationToken);
        }

        var confirmResponse = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/amendments/confirm",
            new { previewToken, confirmed = true },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        await using var verification = app.Services.CreateAsyncScope();
        var bookings = verification.ServiceProvider.GetRequiredService<BookingDbContext>();
        var paymentsAfterContext = verification.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var booking = await bookings.Bookings.AsNoTracking().SingleAsync(
            item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId,
            TestContext.Current.CancellationToken);
        var travellers = await bookings.Travellers.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Position)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        var audits = await bookings.Amendments.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingOccupancy.Triple, booking.Occupancy);
        Assert.Equal(3, booking.TravellerCount);
        Assert.Equal(150_000m, booking.Total);
        Assert.Equal(2, booking.Version);
        Assert.Equal(3, travellers.Length);
        Assert.Equal("Sara Rahman", travellers[2].FullName);
        var audit = Assert.Single(audits);
        Assert.Equal(1, audit.PreviousBookingVersion);
        Assert.Equal(2, audit.ResultingBookingVersion);
        Assert.Equal(50_000m, audit.PriceDelta);
        Assert.Contains("Double", audit.BeforeSnapshotJson, StringComparison.Ordinal);
        Assert.Contains("Triple", audit.AfterSnapshotJson, StringComparison.Ordinal);
        Assert.Equal(
            paymentsBefore,
            await paymentsAfterContext.PaymentAttempts.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            PaymentAttemptState.Succeeded,
            await paymentsAfterContext.PaymentAttempts.AsNoTracking()
                .Select(item => item.State)
                .SingleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Replaying_same_preview_is_rejected_as_stale_without_second_audit()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/amendments/preview",
            OperatorBookingAmendmentApi.DoubleProposal(),
            TestContext.Current.CancellationToken);
        using var previewBody = JsonDocument.Parse(
            await previewResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var previewToken = previewBody.RootElement.GetProperty("previewToken").GetString();

        var first = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/amendments/confirm",
            new { previewToken, confirmed = true },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var replay = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/amendments/confirm",
            new { previewToken, confirmed = true },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Contains(
            "booking_stale",
            await replay.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);

        await using var scope = app.Services.CreateAsyncScope();
        var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Equal(
            1,
            await bookings.Amendments.CountAsync(
                item => item.BookingId == OperatorBookingAmendmentApi.OwnedBookingId,
                TestContext.Current.CancellationToken));
    }
}

internal sealed class OperatorBookingAmendmentApi : WebApplicationFactory<Program>
{
    public const string OperatorAccount = "vs25-operator";
    public static readonly Guid OwnedBookingId = Guid.Parse("61000000-0000-0000-0000-000000000001");
    public static readonly Guid ForeignBookingId = Guid.Parse("61000000-0000-0000-0000-000000000002");
    private static readonly Guid DepartureId = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly Guid PriceVersionId = Guid.Parse("63000000-0000-0000-0000-000000000001");

    private readonly string connection;

    private OperatorBookingAmendmentApi(string connection) => this.connection = connection;

    public static async Task<OperatorBookingAmendmentApi> CreateAsync(
        CancellationToken cancellationToken)
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_COMMERCIAL_TEST_DB",
            "VS-25 operator booking amendment API");
        var app = new OperatorBookingAmendmentApi(connection);

        using var scope = app.Services.CreateScope();
        var operators = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        var catalogue = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        var pricing = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var booking = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        await booking.Database.EnsureDeletedAsync(cancellationToken);
        await operators.Database.MigrateAsync(cancellationToken);
        await catalogue.Database.MigrateAsync(cancellationToken);
        await pricing.Database.MigrateAsync(cancellationToken);
        await booking.Database.MigrateAsync(cancellationToken);
        await payments.Database.MigrateAsync(cancellationToken);

        await SeedAsync(operators, catalogue, pricing, booking, payments, cancellationToken);
        return app;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestSettings.ConfigureTestHost(builder);
        builder.UseSetting("ConnectionStrings:NoorPath", connection);
    }

    public HttpClient CreateClientFor(string accountId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-NoorPath-Test-Identity", accountId);
        return client;
    }

    public static object DoubleProposal() => new
    {
        occupancy = "double",
        travellers = new[]
        {
            new
            {
                travellerId = "64000000-0000-0000-0000-000000000001",
                fullName = "Amina Rahman",
                dateOfBirth = "1987-04-05"
            },
            new
            {
                travellerId = "64000000-0000-0000-0000-000000000002",
                fullName = "Omar Rahman",
                dateOfBirth = "1985-02-11"
            }
        },
        reason = "Correct the booked traveller snapshot."
    };

    public static object TripleProposal() => new
    {
        occupancy = "triple",
        travellers = new[]
        {
            new
            {
                travellerId = "64000000-0000-0000-0000-000000000001",
                fullName = "Amina Rahman",
                dateOfBirth = "1987-04-05"
            },
            new
            {
                travellerId = "64000000-0000-0000-0000-000000000002",
                fullName = "Omar Rahman",
                dateOfBirth = "1985-02-11"
            },
            new
            {
                travellerId = "64000000-0000-0000-0000-000000000003",
                fullName = "Sara Rahman",
                dateOfBirth = "1990-03-10"
            }
        },
        reason = "Add the confirmed third traveller and move to triple sharing."
    };

    private static async Task SeedAsync(
        OperatorsDbContext operators,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        BookingDbContext booking,
        PaymentsDbContext payments,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var membershipId = Guid.Parse("65000000-0000-0000-0000-000000000001");
        operators.Operators.AddRange(
            new OperatorRecord
            {
                Id = "operator-a",
                DisplayName = "VS-25 Operator A",
                State = OperatorState.Approved,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new OperatorRecord
            {
                Id = "operator-b",
                DisplayName = "VS-25 Operator B",
                State = OperatorState.Approved,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        operators.Memberships.Add(new OperatorMembershipRecord
        {
            Id = membershipId,
            OperatorId = "operator-a",
            AccountId = OperatorAccount,
            Status = MembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        operators.MembershipPermissions.Add(new OperatorMembershipPermissionRecord
        {
            Id = Guid.Parse("65000000-0000-0000-0000-000000000002"),
            MembershipId = membershipId,
            Permission = OperatorPermissions.AdminAccess
        });
        await operators.SaveChangesAsync(cancellationToken);

        var templateId = Guid.Parse("66000000-0000-0000-0000-000000000001");
        var packageVersionId = Guid.Parse("66000000-0000-0000-0000-000000000002");
        catalogue.PackageTemplates.Add(new PackageTemplateRecord
        {
            Id = templateId,
            OperatorId = "operator-a",
            WorkingName = "VS-25 Delhi Umrah",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        catalogue.PackageVersions.Add(new PackageVersionRecord
        {
            Id = packageVersionId,
            PackageTemplateId = templateId,
            Sequence = 1,
            Status = CatalogueDraftStatus.Published,
            Name = "VS-25 Delhi Umrah",
            Summary = "Integration-test package",
            MakkahHotelName = "Makkah Hotel",
            MakkahClassification = "4 star",
            MakkahDistanceDisclosure = "Near Haram",
            MakkahNights = 5,
            MakkahConfirmationState = FactConfirmationState.Confirmed,
            MadinahHotelName = "Madinah Hotel",
            MadinahClassification = "4 star",
            MadinahDistanceDisclosure = "Near Masjid",
            MadinahNights = 5,
            MadinahConfirmationState = FactConfirmationState.Confirmed,
            TravelRouteSummary = "Delhi to Jeddah",
            TravelDetails = "Integration test",
            TravelConfirmationState = FactConfirmationState.Confirmed,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        catalogue.DepartureBatches.Add(new DepartureBatchRecord
        {
            Id = DepartureId,
            OperatorId = "operator-a",
            PackageVersionId = packageVersionId,
            Origin = "Delhi (DEL)",
            DepartureDate = new DateOnly(2027, 1, 10),
            ReturnDate = new DateOnly(2027, 1, 21),
            Status = CatalogueDraftStatus.Published,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await catalogue.SaveChangesAsync(cancellationToken);

        var pricePlanId = Guid.Parse("67000000-0000-0000-0000-000000000001");
        pricing.PricePlans.Add(new PricePlanRecord
        {
            Id = pricePlanId,
            DepartureId = DepartureId,
            OperatorId = "operator-a",
            Currency = "INR",
            DepositPercent = 20m,
            InstalmentDayOfMonth = 15,
            FinalPaymentDueDaysBeforeDeparture = 30,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await pricing.SaveChangesAsync(cancellationToken);
        pricing.PriceVersions.Add(new PriceVersionRecord
        {
            Id = PriceVersionId,
            PricePlanId = pricePlanId,
            DepartureId = DepartureId,
            OperatorId = "operator-a",
            SourcePlanVersion = 1,
            Currency = "INR",
            PublishedByAccountId = OperatorAccount,
            PublishedAtUtc = now
        });
        pricing.PublishedOccupancyPrices.AddRange(
            new PublishedOccupancyPriceRecord
            {
                Id = Guid.Parse("67000000-0000-0000-0000-000000000002"),
                PriceVersionId = PriceVersionId,
                Occupancy = PricingOccupancy.Double,
                Amount = 50_000m
            },
            new PublishedOccupancyPriceRecord
            {
                Id = Guid.Parse("67000000-0000-0000-0000-000000000003"),
                PriceVersionId = PriceVersionId,
                Occupancy = PricingOccupancy.Triple,
                Amount = 50_000m
            });
        await pricing.SaveChangesAsync(cancellationToken);

        booking.Bookings.AddRange(
            Booking(OwnedBookingId, "NP-VS25-0001", "operator-a", "owned"),
            Booking(ForeignBookingId, "NP-VS25-FOREIGN", "operator-b", "foreign"));
        booking.Travellers.AddRange(
            Traveller(Guid.Parse("68000000-0000-0000-0000-000000000001"), OwnedBookingId, Guid.Parse("64000000-0000-0000-0000-000000000001"), 1, "Amina Rahman", new DateOnly(1987, 4, 5)),
            Traveller(Guid.Parse("68000000-0000-0000-0000-000000000002"), OwnedBookingId, Guid.Parse("64000000-0000-0000-0000-000000000002"), 2, "Omar Rahman", new DateOnly(1985, 2, 11)));
        booking.Instalments.Add(new BookingInstalmentRecord
        {
            Id = Guid.Parse("69000000-0000-0000-0000-000000000001"),
            BookingId = OwnedBookingId,
            Sequence = 1,
            DueDate = new DateOnly(2026, 12, 1),
            Amount = 80_000m
        });
        await booking.SaveChangesAsync(cancellationToken);

        payments.PaymentAttempts.Add(new PaymentAttemptRecord
        {
            Id = Guid.Parse("6a000000-0000-0000-0000-000000000001"),
            BookingId = OwnedBookingId,
            AccountId = "customer-a",
            Currency = "INR",
            Amount = 20_000m,
            State = PaymentAttemptState.Succeeded,
            Provider = "test",
            ProviderSessionId = "vs25-session",
            ProviderPaymentId = "vs25-payment",
            IdempotencyKeyHash = "payment",
            RequestFingerprint = "payment",
            CorrelationId = "payment",
            CreatedAtUtc = now.AddDays(-1),
            UpdatedAtUtc = now,
            CheckoutExpiresAtUtc = now.AddHours(1),
            SettledAtUtc = now
        });
        await payments.SaveChangesAsync(cancellationToken);
    }

    private static BookingRecord Booking(
        Guid id,
        string reference,
        string operatorId,
        string suffix)
    {
        var now = DateTimeOffset.UtcNow;
        return new BookingRecord
        {
            Id = id,
            Reference = reference,
            AccountId = $"customer-{suffix}",
            OperatorId = operatorId,
            DepartureId = DepartureId,
            QuoteId = Guid.NewGuid(),
            PriceVersionId = PriceVersionId,
            InventoryHoldId = Guid.NewGuid(),
            Occupancy = BookingOccupancy.Double,
            TravellerCount = 2,
            Currency = "INR",
            UnitPrice = 50_000m,
            Total = 100_000m,
            DueNow = 20_000m,
            Remaining = 80_000m,
            State = BookingState.Confirmed,
            IdempotencyKeyHash = $"idempotency-{suffix}",
            RequestFingerprint = $"fingerprint-{suffix}",
            CorrelationId = $"correlation-{suffix}",
            Version = 1,
            CreatedAtUtc = now.AddDays(-2),
            UpdatedAtUtc = now,
            ConfirmedAtUtc = now.AddDays(-1)
        };
    }

    private static BookingTravellerRecord Traveller(
        Guid id,
        Guid bookingId,
        Guid travellerId,
        int position,
        string fullName,
        DateOnly dateOfBirth) =>
        new()
        {
            Id = id,
            BookingId = bookingId,
            TravellerId = travellerId,
            Position = position,
            FullName = fullName,
            DateOfBirth = dateOfBirth
        };
}
