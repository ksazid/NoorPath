using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Testing;
using Xunit;

namespace NoorPath.Catalogue.Integration.Tests;

public sealed class CataloguePersistenceTests
{
    [Fact]
    public async Task Migration_applies_and_draft_authoring_is_durable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_CATALOGUE_TEST_DB",
            "Catalogue persistence");
        var options = new DbContextOptionsBuilder<CatalogueDbContext>()
            .UseNpgsql(connection)
            .Options;

        await using var db = new CatalogueDbContext(options);
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var template = new PackageTemplateRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = "operator-noor",
            WorkingName = "Noor Harmony",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var packageVersion = new PackageVersionRecord
        {
            Id = Guid.NewGuid(),
            PackageTemplateId = template.Id,
            Sequence = 1,
            Status = CatalogueDraftStatus.Draft,
            Name = "Noor Harmony",
            Summary = "A factual Umrah journey draft.",
            MakkahHotelName = "Makkah Hotel",
            MakkahClassification = "4 star",
            MakkahDistanceDisclosure = "850 m from Masjid al-Haram",
            MakkahNights = 6,
            MakkahConfirmationState = FactConfirmationState.Confirmed,
            MadinahHotelName = "Madinah Hotel",
            MadinahClassification = "4 star",
            MadinahDistanceDisclosure = "450 m from Al-Masjid an-Nabawi",
            MadinahNights = 5,
            MadinahConfirmationState = FactConfirmationState.Pending,
            TravelRouteSummary = "Delhi → Jeddah → Makkah → Madinah",
            TravelDetails = "Flight details pending final confirmation.",
            TravelConfirmationState = FactConfirmationState.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var departure = new DepartureBatchRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = template.OperatorId,
            PackageVersionId = packageVersion.Id,
            Origin = "Delhi (DEL)",
            DepartureDate = new(2026, 10, 10),
            ReturnDate = new(2026, 10, 22),
            Status = CatalogueDraftStatus.Draft,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var audit = new CatalogueDraftAuditRecord
        {
            Id = Guid.NewGuid(),
            DepartureBatchId = departure.Id,
            ActorAccountId = "catalogue-author",
            CorrelationId = "persistence-test",
            Action = "created",
            Version = 1,
            Timestamp = now
        };

        db.AddRange(template, packageVersion, departure, audit);
        db.PackageContentItems.Add(new()
        {
            Id = Guid.NewGuid(),
            PackageVersionId = packageVersion.Id,
            Kind = PackageContentKind.Inclusion,
            Position = 0,
            Text = "Return flights"
        });
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var savedDeparture = await db.DepartureBatches.SingleAsync(cancellationToken);
        Assert.Equal(CatalogueDraftStatus.Draft, savedDeparture.Status);
        Assert.Equal("operator-noor", savedDeparture.OperatorId);
        Assert.Single(await db.DraftAudits.ToListAsync(cancellationToken));
        Assert.Single(await db.PackageContentItems.ToListAsync(cancellationToken));
    }
}
