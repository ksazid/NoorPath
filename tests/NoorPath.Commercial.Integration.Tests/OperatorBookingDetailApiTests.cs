using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Documents;
using NoorPath.Documents.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using NoorPath.Testing;
using NoorPath.Visa;
using NoorPath.Visa.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class OperatorBookingDetailApiTests
{
    [Fact]
    public async Task Owned_booking_returns_composed_operational_detail()
    {
        await using var app = await OperatorBookingDetailApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingDetailApi.OperatorAccount);

        var response = await client.GetAsync(
            $"/api/v1/operator/bookings/{OperatorBookingDetailApi.OwnedBookingId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        var root = body.RootElement;

        Assert.Equal("NP-VS24-0001", root.GetProperty("reference").GetString());
        Assert.Equal("VS-24 Delhi Umrah", root.GetProperty("packageName").GetString());
        Assert.Equal("confirmed", root.GetProperty("state").GetString());
        Assert.Equal(2, root.GetProperty("travellerCount").GetInt32());
        Assert.Equal(100_000m, root.GetProperty("payment").GetProperty("total").GetDecimal());
        Assert.Equal(50_000m, root.GetProperty("payment").GetProperty("paid").GetDecimal());
        Assert.Equal(2, root.GetProperty("payment").GetProperty("instalments").GetArrayLength());
        Assert.Equal(2, root.GetProperty("travellers").GetArrayLength());
        Assert.Equal(2, root.GetProperty("documents").GetProperty("required").GetInt32());
        Assert.Equal(1, root.GetProperty("documents").GetProperty("approved").GetInt32());
        Assert.Equal(1, root.GetProperty("visa").GetProperty("approved").GetInt32());
    }

    [Fact]
    public async Task Foreign_operator_booking_is_safe_not_found()
    {
        await using var app = await OperatorBookingDetailApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingDetailApi.OperatorAccount);

        var response = await client.GetAsync(
            $"/api/v1/operator/bookings/{OperatorBookingDetailApi.ForeignBookingId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("operator-b", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NP-VS24-FOREIGN", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_booking_is_safe_not_found()
    {
        await using var app = await OperatorBookingDetailApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingDetailApi.OperatorAccount);

        var response = await client.GetAsync(
            $"/api/v1/operator/bookings/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Booking_detail_requires_operator_authority()
    {
        await using var app = await OperatorBookingDetailApi.CreateAsync(
            TestContext.Current.CancellationToken);

        var unauthenticated = await app.CreateClient().GetAsync(
            $"/api/v1/operator/bookings/{OperatorBookingDetailApi.OwnedBookingId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        using var customer = app.CreateClientFor("customer-only");
        var forbidden = await customer.GetAsync(
            $"/api/v1/operator/bookings/{OperatorBookingDetailApi.OwnedBookingId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}

internal sealed class OperatorBookingDetailApi : WebApplicationFactory<Program>
{
    public const string OperatorAccount = "vs24-operator";
    public static readonly Guid OwnedBookingId = Guid.Parse("51000000-0000-0000-0000-000000000001");
    public static readonly Guid ForeignBookingId = Guid.Parse("51000000-0000-0000-0000-000000000002");

    private readonly string connection;

    private OperatorBookingDetailApi(string connection) => this.connection = connection;

    public static async Task<OperatorBookingDetailApi> CreateAsync(CancellationToken cancellationToken)
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_COMMERCIAL_TEST_DB",
            "VS-24 operator booking detail API");
        var app = new OperatorBookingDetailApi(connection);

        using var scope = app.Services.CreateScope();
        var operators = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        var catalogue = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        var booking = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        var visa = scope.ServiceProvider.GetRequiredService<VisaDbContext>();

        await booking.Database.EnsureDeletedAsync(cancellationToken);
        await operators.Database.MigrateAsync(cancellationToken);
        await catalogue.Database.MigrateAsync(cancellationToken);
        await booking.Database.MigrateAsync(cancellationToken);
        await payments.Database.MigrateAsync(cancellationToken);
        await documents.Database.MigrateAsync(cancellationToken);
        await visa.Database.MigrateAsync(cancellationToken);

        await SeedAsync(operators, catalogue, booking, payments, documents, visa, cancellationToken);
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

    private static async Task SeedAsync(
        OperatorsDbContext operators,
        CatalogueDbContext catalogue,
        BookingDbContext booking,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var membershipId = Guid.Parse("52000000-0000-0000-0000-000000000001");
        operators.Operators.AddRange(
            new OperatorRecord
            {
                Id = "operator-a",
                DisplayName = "VS-24 Operator A",
                State = OperatorState.Approved,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new OperatorRecord
            {
                Id = "operator-b",
                DisplayName = "VS-24 Operator B",
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
            Id = Guid.Parse("52000000-0000-0000-0000-000000000002"),
            MembershipId = membershipId,
            Permission = OperatorPermissions.AdminAccess
        });
        await operators.SaveChangesAsync(cancellationToken);

        var templateId = Guid.Parse("53000000-0000-0000-0000-000000000001");
        var versionId = Guid.Parse("53000000-0000-0000-0000-000000000002");
        var departureId = Guid.Parse("53000000-0000-0000-0000-000000000003");
        catalogue.PackageTemplates.Add(new PackageTemplateRecord
        {
            Id = templateId,
            OperatorId = "operator-a",
            WorkingName = "VS-24 Delhi Umrah",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        catalogue.PackageVersions.Add(new PackageVersionRecord
        {
            Id = versionId,
            PackageTemplateId = templateId,
            Sequence = 1,
            Status = CatalogueDraftStatus.Published,
            Name = "VS-24 Delhi Umrah",
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
            Id = departureId,
            OperatorId = "operator-a",
            PackageVersionId = versionId,
            Origin = "Delhi (DEL)",
            DepartureDate = new DateOnly(2027, 1, 10),
            ReturnDate = new DateOnly(2027, 1, 21),
            Status = CatalogueDraftStatus.Published,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await catalogue.SaveChangesAsync(cancellationToken);

        var travellerOne = Guid.Parse("54000000-0000-0000-0000-000000000001");
        var travellerTwo = Guid.Parse("54000000-0000-0000-0000-000000000002");
        booking.Bookings.AddRange(
            new BookingRecord
            {
                Id = OwnedBookingId,
                Reference = "NP-VS24-0001",
                AccountId = "customer-a",
                OperatorId = "operator-a",
                DepartureId = departureId,
                QuoteId = Guid.Parse("55000000-0000-0000-0000-000000000001"),
                PriceVersionId = Guid.Parse("55000000-0000-0000-0000-000000000002"),
                InventoryHoldId = Guid.Parse("55000000-0000-0000-0000-000000000003"),
                Occupancy = BookingOccupancy.Double,
                TravellerCount = 2,
                Currency = "INR",
                UnitPrice = 50_000m,
                Total = 100_000m,
                DueNow = 50_000m,
                Remaining = 50_000m,
                State = BookingState.Confirmed,
                IdempotencyKeyHash = "owned",
                RequestFingerprint = "owned",
                CorrelationId = "owned",
                CreatedAtUtc = now.AddDays(-2),
                UpdatedAtUtc = now,
                ConfirmedAtUtc = now.AddDays(-1)
            },
            new BookingRecord
            {
                Id = ForeignBookingId,
                Reference = "NP-VS24-FOREIGN",
                AccountId = "customer-b",
                OperatorId = "operator-b",
                DepartureId = departureId,
                QuoteId = Guid.Parse("55000000-0000-0000-0000-000000000004"),
                PriceVersionId = Guid.Parse("55000000-0000-0000-0000-000000000005"),
                InventoryHoldId = Guid.Parse("55000000-0000-0000-0000-000000000006"),
                Occupancy = BookingOccupancy.Double,
                TravellerCount = 2,
                Currency = "INR",
                UnitPrice = 50_000m,
                Total = 100_000m,
                DueNow = 50_000m,
                Remaining = 50_000m,
                State = BookingState.Confirmed,
                IdempotencyKeyHash = "foreign",
                RequestFingerprint = "foreign",
                CorrelationId = "foreign",
                CreatedAtUtc = now.AddDays(-2),
                UpdatedAtUtc = now
            });
        booking.Travellers.AddRange(
            new BookingTravellerRecord
            {
                Id = Guid.Parse("56000000-0000-0000-0000-000000000001"),
                BookingId = OwnedBookingId,
                TravellerId = travellerOne,
                Position = 1,
                FullName = "Amina Rahman",
                DateOfBirth = new DateOnly(1987, 4, 5)
            },
            new BookingTravellerRecord
            {
                Id = Guid.Parse("56000000-0000-0000-0000-000000000002"),
                BookingId = OwnedBookingId,
                TravellerId = travellerTwo,
                Position = 2,
                FullName = "Omar Rahman",
                DateOfBirth = new DateOnly(1985, 2, 11)
            });
        booking.Instalments.AddRange(
            new BookingInstalmentRecord
            {
                Id = Guid.Parse("57000000-0000-0000-0000-000000000001"),
                BookingId = OwnedBookingId,
                Sequence = 1,
                DueDate = new DateOnly(2026, 10, 1),
                Amount = 25_000m
            },
            new BookingInstalmentRecord
            {
                Id = Guid.Parse("57000000-0000-0000-0000-000000000002"),
                BookingId = OwnedBookingId,
                Sequence = 2,
                DueDate = new DateOnly(2026, 12, 1),
                Amount = 25_000m
            });
        await booking.SaveChangesAsync(cancellationToken);

        payments.PaymentAttempts.Add(new PaymentAttemptRecord
        {
            Id = Guid.Parse("58000000-0000-0000-0000-000000000001"),
            BookingId = OwnedBookingId,
            AccountId = "customer-a",
            Currency = "INR",
            Amount = 50_000m,
            State = PaymentAttemptState.Succeeded,
            Provider = "test",
            ProviderSessionId = "vs24-session",
            ProviderPaymentId = "vs24-payment",
            IdempotencyKeyHash = "payment",
            RequestFingerprint = "payment",
            CorrelationId = "payment",
            CreatedAtUtc = now.AddDays(-1),
            UpdatedAtUtc = now,
            CheckoutExpiresAtUtc = now.AddHours(1),
            SettledAtUtc = now
        });
        await payments.SaveChangesAsync(cancellationToken);

        var requirementOne = Guid.Parse("59000000-0000-0000-0000-000000000001");
        var requirementTwo = Guid.Parse("59000000-0000-0000-0000-000000000002");
        documents.Requirements.AddRange(
            new DocumentRequirementRecord
            {
                Id = requirementOne,
                BookingId = OwnedBookingId,
                TravellerId = travellerOne,
                Kind = DocumentKind.PassportBioPage,
                CreatedAtUtc = now
            },
            new DocumentRequirementRecord
            {
                Id = requirementTwo,
                BookingId = OwnedBookingId,
                TravellerId = travellerTwo,
                Kind = DocumentKind.PassportBioPage,
                CreatedAtUtc = now
            });
        documents.Submissions.Add(new DocumentSubmissionRecord
        {
            Id = Guid.Parse("5a000000-0000-0000-0000-000000000001"),
            RequirementId = requirementOne,
            ObjectKey = "vs24/passport-one",
            DeclaredContentType = "application/pdf",
            DeclaredSize = 100,
            State = SubmissionState.Approved,
            MalwareStatus = MalwareStatus.Safe,
            CreatedAtUtc = now,
            Version = 1
        });
        await documents.SaveChangesAsync(cancellationToken);

        visa.Cases.AddRange(
            new VisaCaseRecord
            {
                Id = Guid.Parse("5b000000-0000-0000-0000-000000000001"),
                BookingId = OwnedBookingId,
                TravellerId = travellerOne,
                OperatorId = "operator-a",
                Status = VisaStatus.Approved,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new VisaCaseRecord
            {
                Id = Guid.Parse("5b000000-0000-0000-0000-000000000002"),
                BookingId = OwnedBookingId,
                TravellerId = travellerTwo,
                OperatorId = "operator-a",
                Status = VisaStatus.ActionRequired,
                CustomerAction = "Upload a corrected passport scan.",
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        await visa.SaveChangesAsync(cancellationToken);
    }
}
