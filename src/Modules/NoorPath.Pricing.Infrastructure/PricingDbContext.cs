using Microsoft.EntityFrameworkCore;
using NoorPath.Pricing;

namespace NoorPath.Pricing.Infrastructure;

public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public DbSet<PricePlanRecord> PricePlans => Set<PricePlanRecord>();
    public DbSet<OccupancyPriceRecord> OccupancyPrices => Set<OccupancyPriceRecord>();
    public DbSet<PricingAuditRecord> Audits => Set<PricingAuditRecord>();
    public DbSet<PriceVersionRecord> PriceVersions => Set<PriceVersionRecord>();
    public DbSet<PublishedOccupancyPriceRecord> PublishedOccupancyPrices => Set<PublishedOccupancyPriceRecord>();
    public DbSet<QuoteRecord> Quotes => Set<QuoteRecord>();
    public DbSet<QuoteTravellerRecord> QuoteTravellers => Set<QuoteTravellerRecord>();
    public DbSet<QuoteInstalmentRecord> QuoteInstalments => Set<QuoteInstalmentRecord>();

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
            entity.Property(x => x.DepositPercent).HasPrecision(5, 2);
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

        modelBuilder.Entity<PriceVersionRecord>(entity =>
        {
            entity.ToTable("price_versions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.PublishedByAccountId).HasMaxLength(120);
            entity.Property(x => x.DepositPercent).HasPrecision(5, 2);
            entity.HasIndex(x => x.DepartureId).IsUnique();
            entity.HasIndex(x => new { x.PricePlanId, x.SourcePlanVersion }).IsUnique();
            entity.HasOne<PricePlanRecord>()
                .WithMany()
                .HasForeignKey(x => x.PricePlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PublishedOccupancyPriceRecord>(entity =>
        {
            entity.ToTable("published_occupancy_prices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Occupancy).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.PriceVersionId, x.Occupancy }).IsUnique();
            entity.HasOne<PriceVersionRecord>()
                .WithMany()
                .HasForeignKey(x => x.PriceVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuoteRecord>(entity =>
        {
            entity.ToTable("quotes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Occupancy).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.Property(x => x.DueNow).HasPrecision(18, 2);
            entity.Property(x => x.Remaining).HasPrecision(18, 2);
            entity.HasIndex(x => x.AccountId);
            entity.HasIndex(x => x.DepartureId);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasOne<PriceVersionRecord>()
                .WithMany()
                .HasForeignKey(x => x.PriceVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuoteTravellerRecord>(entity =>
        {
            entity.ToTable("quote_travellers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.QuoteId, x.Position }).IsUnique();
            entity.HasIndex(x => new { x.QuoteId, x.TravellerId }).IsUnique();
            entity.HasOne<QuoteRecord>()
                .WithMany()
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuoteInstalmentRecord>(entity =>
        {
            entity.ToTable("quote_instalments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.QuoteId, x.Sequence }).IsUnique();
            entity.HasOne<QuoteRecord>()
                .WithMany()
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public sealed class PricePlanRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public required string Currency { get; set; }
    public decimal? DepositPercent { get; set; }
    public int? InstalmentDayOfMonth { get; set; }
    public int? FinalPaymentDueDaysBeforeDeparture { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public PaymentPlanDefinition? PaymentPlan => DepositPercent is null
        ? null
        : new(DepositPercent.Value, InstalmentDayOfMonth!.Value, FinalPaymentDueDaysBeforeDeparture!.Value);
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

public sealed class PriceVersionRecord
{
    public Guid Id { get; set; }
    public Guid PricePlanId { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public int SourcePlanVersion { get; set; }
    public required string Currency { get; set; }
    public decimal? DepositPercent { get; set; }
    public int? InstalmentDayOfMonth { get; set; }
    public int? FinalPaymentDueDaysBeforeDeparture { get; set; }
    public required string PublishedByAccountId { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }

    public PaymentPlanDefinition? PaymentPlan => DepositPercent is null
        ? null
        : new(DepositPercent.Value, InstalmentDayOfMonth!.Value, FinalPaymentDueDaysBeforeDeparture!.Value);
}

public sealed class PublishedOccupancyPriceRecord
{
    public Guid Id { get; set; }
    public Guid PriceVersionId { get; set; }
    public PricingOccupancy Occupancy { get; set; }
    public decimal Amount { get; set; }
}

public sealed class QuoteRecord
{
    public Guid Id { get; set; }
    public required string AccountId { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public Guid PriceVersionId { get; set; }
    public PricingOccupancy Occupancy { get; set; }
    public int TravellerCount { get; set; }
    public required string Currency { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public decimal DueNow { get; set; }
    public decimal Remaining { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class QuoteTravellerRecord
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid TravellerId { get; set; }
    public int Position { get; set; }
}

public sealed class QuoteInstalmentRecord
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public int Sequence { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
}
