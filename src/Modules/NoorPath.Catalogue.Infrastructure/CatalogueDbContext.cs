using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;

namespace NoorPath.Catalogue.Infrastructure;

public sealed class CatalogueDbContext(DbContextOptions<CatalogueDbContext> options) : DbContext(options)
{
    public DbSet<PackageTemplateRecord> PackageTemplates => Set<PackageTemplateRecord>();
    public DbSet<PackageVersionRecord> PackageVersions => Set<PackageVersionRecord>();
    public DbSet<DepartureBatchRecord> DepartureBatches => Set<DepartureBatchRecord>();
    public DbSet<PackageContentItemRecord> PackageContentItems => Set<PackageContentItemRecord>();
    public DbSet<CatalogueDraftAuditRecord> DraftAudits => Set<CatalogueDraftAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalogue");

        modelBuilder.Entity<PackageTemplateRecord>(entity =>
        {
            entity.ToTable("package_templates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.WorkingName).HasMaxLength(120);
            entity.HasIndex(x => x.OperatorId);
        });

        modelBuilder.Entity<PackageVersionRecord>(entity =>
        {
            entity.ToTable("package_versions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Summary).HasMaxLength(600);
            entity.Property(x => x.MakkahHotelName).HasMaxLength(160);
            entity.Property(x => x.MakkahClassification).HasMaxLength(80);
            entity.Property(x => x.MakkahDistanceDisclosure).HasMaxLength(120);
            entity.Property(x => x.MakkahConfirmationState).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.MadinahHotelName).HasMaxLength(160);
            entity.Property(x => x.MadinahClassification).HasMaxLength(80);
            entity.Property(x => x.MadinahDistanceDisclosure).HasMaxLength(120);
            entity.Property(x => x.MadinahConfirmationState).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.TravelRouteSummary).HasMaxLength(200);
            entity.Property(x => x.TravelDetails).HasMaxLength(600);
            entity.Property(x => x.TravelConfirmationState).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => new { x.PackageTemplateId, x.Sequence }).IsUnique();
            entity.HasOne<PackageTemplateRecord>().WithMany().HasForeignKey(x => x.PackageTemplateId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DepartureBatchRecord>(entity =>
        {
            entity.ToTable("departure_batches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Origin).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.OperatorId);
            entity.HasIndex(x => x.PackageVersionId);
            entity.HasOne<PackageVersionRecord>().WithMany().HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PackageContentItemRecord>(entity =>
        {
            entity.ToTable("package_content_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Text).HasMaxLength(120);
            entity.HasIndex(x => new { x.PackageVersionId, x.Kind, x.Position }).IsUnique();
            entity.HasOne<PackageVersionRecord>().WithMany().HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogueDraftAuditRecord>(entity =>
        {
            entity.ToTable("draft_audits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Action).HasMaxLength(20);
            entity.HasIndex(x => new { x.DepartureBatchId, x.Version });
            entity.HasOne<DepartureBatchRecord>().WithMany().HasForeignKey(x => x.DepartureBatchId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class PackageTemplateRecord
{
    public Guid Id { get; set; }
    public required string OperatorId { get; set; }
    public required string WorkingName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class PackageVersionRecord
{
    public Guid Id { get; set; }
    public Guid PackageTemplateId { get; set; }
    public int Sequence { get; set; } = 1;
    public CatalogueDraftStatus Status { get; set; } = CatalogueDraftStatus.Draft;
    public required string Name { get; set; }
    public required string Summary { get; set; }
    public required string MakkahHotelName { get; set; }
    public required string MakkahClassification { get; set; }
    public required string MakkahDistanceDisclosure { get; set; }
    public int MakkahNights { get; set; }
    public FactConfirmationState MakkahConfirmationState { get; set; }
    public required string MadinahHotelName { get; set; }
    public required string MadinahClassification { get; set; }
    public required string MadinahDistanceDisclosure { get; set; }
    public int MadinahNights { get; set; }
    public FactConfirmationState MadinahConfirmationState { get; set; }
    public required string TravelRouteSummary { get; set; }
    public required string TravelDetails { get; set; }
    public FactConfirmationState TravelConfirmationState { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class DepartureBatchRecord
{
    public Guid Id { get; set; }
    public required string OperatorId { get; set; }
    public Guid PackageVersionId { get; set; }
    public required string Origin { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public CatalogueDraftStatus Status { get; set; } = CatalogueDraftStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class PackageContentItemRecord
{
    public Guid Id { get; set; }
    public Guid PackageVersionId { get; set; }
    public PackageContentKind Kind { get; set; }
    public int Position { get; set; }
    public required string Text { get; set; }
}

public sealed class CatalogueDraftAuditRecord
{
    public Guid Id { get; set; }
    public Guid DepartureBatchId { get; set; }
    public required string ActorAccountId { get; set; }
    public required string CorrelationId { get; set; }
    public required string Action { get; set; }
    public int Version { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
