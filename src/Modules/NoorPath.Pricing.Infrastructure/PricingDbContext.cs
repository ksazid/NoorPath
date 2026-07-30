using Microsoft.EntityFrameworkCore;
using NoorPath.Pricing;

namespace NoorPath.Pricing.Infrastructure;

public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public DbSet<PricePlanRecord> PricePlans => Set<PricePlanRecord>();
    public DbSet<OccupancyPriceRecord> OccupancyPrices => Set<OccupancyPriceRecord>();
    public DbSet<PricingAuditRecord> Audits => Set<PricingAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pricing");

        modelBuilder.Entity<PricePlanRecord>(entity =>
        {
            entity.ToTable("price_plans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.DepartureId).IsUnique();
            entity.HasIndex(x => x.OperatorId);
        });

        modelBuilder.Entity<OccupancyPriceRecord>(entity =>
        {
            entity.ToTable("occupancy_prices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Occupancy).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.PricePlanId, x.Occupancy }).IsUnique();
            entity.HasOne<PricePlanRecord>()
                .WithMany()
                .HasForeignKey(x => x.PricePlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PricingAuditRecord>(entity =>
        {
            entity.ToTable("pricing_audits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Action).HasMaxLength(20);
            entity.HasIndex(x => new { x.DepartureId, x.Version });
            entity.HasOne<PricePlanRecord>()
                .WithMany()
                .HasForeignKey(x => x.PricePlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class PricePlanRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public required string Currency { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OccupancyPriceRecord
{
    public Guid Id { get; set; }
    public Guid PricePlanId { get; set; }
    public PricingOccupancy Occupancy { get; set; }
    public decimal Amount { get; set; }
}

public sealed class PricingAuditRecord
{
    public Guid Id { get; set; }
    public Guid PricePlanId { get; set; }
    public Guid DepartureId { get; set; }
    public required string ActorAccountId { get; set; }
    public required string CorrelationId { get; set; }
    public required string Action { get; set; }
    public int Version { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
