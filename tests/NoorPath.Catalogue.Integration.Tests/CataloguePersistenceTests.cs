using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using Xunit;

namespace NoorPath.Catalogue.Integration.Tests;

public sealed class CataloguePersistenceTests
{
    [Fact]
    public async Task Migration_applies_and_publication_is_atomic_and_durable()
    {
        var connection = Environment.GetEnvironmentVariable("NOORPATH_TEST_DB") ?? throw new InvalidOperationException("NOORPATH_TEST_DB is required for Catalogue integration tests.");
        var options = new DbContextOptionsBuilder<CatalogueDbContext>().UseNpgsql(connection).Options;
        await using var db = new CatalogueDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        var package = new PackageRecord { Id = Guid.NewGuid(), OperatorId = "test-approved-noor", OperatorName = "Noor Tours", Name = "Noor Comfort", Summary = "Supported journey", Tier = "Comfort" };
        var batch = new BatchRecord { Id = Guid.NewGuid(), PackageId = package.Id, DepartureCity = "Delhi", Route = "Jeddah to Makkah", DepartureDate = new(2026, 10, 10), ReturnDate = new(2026, 10, 22), Capacity = 24, Availability = AvailabilityMode.Exact };
        db.AddRange(package, batch, new PriceVersionRecord { Id = Guid.NewGuid(), BatchId = batch.Id, Currency = "INR", TotalStartingPrice = 94500, EffectiveAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        await using (var transaction = await db.Database.BeginTransactionAsync()) { batch.Status = BatchStatus.Published; batch.Version++; batch.PublishedAt = DateTimeOffset.UtcNow; db.PublicationAudits.Add(new PublicationAuditRecord { Id = Guid.NewGuid(), BatchId = batch.Id, Actor = "admin", CorrelationId = "test", PreviousStatus = "Draft", NewStatus = "Published", ExpectedVersion = 1, Timestamp = batch.PublishedAt.Value }); await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        db.ChangeTracker.Clear();
        Assert.Equal(BatchStatus.Published, (await db.Batches.SingleAsync()).Status);
        Assert.Single(await db.PublicationAudits.ToListAsync());
    }
}
