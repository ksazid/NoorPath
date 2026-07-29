using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;

namespace NoorPath.Catalogue.Infrastructure;

public sealed class CatalogueDbContext(DbContextOptions<CatalogueDbContext> options) : DbContext(options)
{
    public DbSet<PackageRecord> Packages => Set<PackageRecord>();
    public DbSet<BatchRecord> Batches => Set<BatchRecord>();
    public DbSet<PriceVersionRecord> PriceVersions => Set<PriceVersionRecord>();
    public DbSet<InclusionRecord> Inclusions => Set<InclusionRecord>();
    public DbSet<PublicationAuditRecord> PublicationAudits => Set<PublicationAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalogue");
        modelBuilder.Entity<PackageRecord>(entity => { entity.ToTable("packages"); entity.HasKey(x => x.Id); entity.Property(x => x.OperatorId).HasMaxLength(80); entity.Property(x => x.OperatorName).HasMaxLength(120); entity.Property(x => x.Name).HasMaxLength(120); entity.Property(x => x.Summary).HasMaxLength(300); entity.Property(x => x.Tier).HasMaxLength(40); });
        modelBuilder.Entity<BatchRecord>(entity => { entity.ToTable("batches"); entity.HasKey(x => x.Id); entity.Property(x => x.DepartureCity).HasMaxLength(80); entity.Property(x => x.Route).HasMaxLength(160); entity.Property(x => x.Version).IsConcurrencyToken(); entity.HasOne<PackageRecord>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<PriceVersionRecord>(entity => { entity.ToTable("price_versions"); entity.HasKey(x => x.Id); entity.Property(x => x.Currency).HasMaxLength(3); entity.Property(x => x.TotalStartingPrice).HasPrecision(12, 2); entity.HasOne<BatchRecord>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<InclusionRecord>(entity => { entity.ToTable("inclusions"); entity.HasKey(x => x.Id); entity.Property(x => x.Text).HasMaxLength(80); entity.HasIndex(x => new { x.PackageId, x.Position }).IsUnique(); entity.HasOne<PackageRecord>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<PublicationAuditRecord>(entity => { entity.ToTable("publication_audits"); entity.HasKey(x => x.Id); entity.Property(x => x.Actor).HasMaxLength(100); entity.Property(x => x.CorrelationId).HasMaxLength(100); entity.Property(x => x.PreviousStatus).HasMaxLength(20); entity.Property(x => x.NewStatus).HasMaxLength(20); entity.HasIndex(x => x.BatchId).IsUnique(); entity.HasOne<BatchRecord>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict); });
    }
}

public sealed class PackageRecord { public Guid Id { get; set; } public required string OperatorId { get; set; } public required string OperatorName { get; set; } public required string Name { get; set; } public required string Summary { get; set; } public required string Tier { get; set; } }
public sealed class BatchRecord { public Guid Id { get; set; } public Guid PackageId { get; set; } public required string DepartureCity { get; set; } public required string Route { get; set; } public DateOnly DepartureDate { get; set; } public DateOnly ReturnDate { get; set; } public int Capacity { get; set; } public AvailabilityMode Availability { get; set; } public BatchStatus Status { get; set; } = BatchStatus.Draft; public int Version { get; set; } = 1; public DateTimeOffset? PublishedAt { get; set; } }
public sealed class PriceVersionRecord { public Guid Id { get; set; } public Guid BatchId { get; set; } public required string Currency { get; set; } = "INR"; public decimal TotalStartingPrice { get; set; } public DateTimeOffset EffectiveAt { get; set; } public int Version { get; set; } = 1; }
public sealed class InclusionRecord { public Guid Id { get; set; } public Guid PackageId { get; set; } public int Position { get; set; } public required string Text { get; set; } }
public sealed class PublicationAuditRecord { public Guid Id { get; set; } public Guid BatchId { get; set; } public required string Actor { get; set; } public required string CorrelationId { get; set; } public required string PreviousStatus { get; set; } public required string NewStatus { get; set; } public int ExpectedVersion { get; set; } public DateTimeOffset Timestamp { get; set; } }
