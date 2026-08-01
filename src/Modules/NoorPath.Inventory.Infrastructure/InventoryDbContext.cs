using Microsoft.EntityFrameworkCore;
using NoorPath.Inventory;

namespace NoorPath.Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryConfigurationRecord> Configurations => Set<InventoryConfigurationRecord>();
    public DbSet<InventoryPoolRecord> Pools => Set<InventoryPoolRecord>();
    public DbSet<InventoryAuditRecord> Audits => Set<InventoryAuditRecord>();
    public DbSet<InventoryHoldRecord> Holds => Set<InventoryHoldRecord>();
    public DbSet<InventoryCommitmentRecord> Commitments => Set<InventoryCommitmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.Entity<InventoryConfigurationRecord>(entity =>
        {
            entity.ToTable("inventory_configurations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.DepartureId).IsUnique();
            entity.HasIndex(x => x.OperatorId);
        });

        modelBuilder.Entity<InventoryPoolRecord>(entity =>
        {
            entity.ToTable("inventory_pools");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Occupancy).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => new { x.InventoryConfigurationId, x.Occupancy }).IsUnique();
            entity.HasOne<InventoryConfigurationRecord>()
                .WithMany()
                .HasForeignKey(x => x.InventoryConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryAuditRecord>(entity =>
        {
            entity.ToTable("inventory_audits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Reason).HasMaxLength(240);
            entity.Property(x => x.Action).HasMaxLength(20);
            entity.HasIndex(x => new { x.DepartureId, x.Version });
            entity.HasOne<InventoryConfigurationRecord>()
                .WithMany()
                .HasForeignKey(x => x.InventoryConfigurationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryHoldRecord>(entity =>
        {
            entity.ToTable("inventory_holds", table =>
            {
                table.HasCheckConstraint(
                    "CK_inventory_holds_Quantity_Positive",
                    "\"Quantity\" > 0");
                table.HasCheckConstraint(
                    "CK_inventory_holds_Expiry_After_Creation",
                    "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
                table.HasCheckConstraint(
                    "CK_inventory_holds_State",
                    "\"State\" IN ('Active', 'Released', 'Expired', 'Committed')");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Occupancy).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.State).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.IdempotencyKeyHash).HasMaxLength(64);
            entity.Property(x => x.RequestFingerprint).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.AccountId, x.IdempotencyKeyHash }).IsUnique();
            entity.HasIndex(x => new { x.InventoryPoolId, x.State, x.ExpiresAtUtc });
            entity.HasIndex(x => new { x.AccountId, x.DepartureId, x.Occupancy, x.State });
            entity.HasIndex(x => new { x.QuoteId, x.State });
            entity.HasIndex(x => x.QuoteId)
                .HasFilter("\"State\" = 'Active'")
                .IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.DepartureId, x.Occupancy })
                .HasFilter("\"State\" = 'Active'")
                .IsUnique();
            entity.HasOne<InventoryPoolRecord>()
                .WithMany()
                .HasForeignKey(x => x.InventoryPoolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryCommitmentRecord>(entity =>
        {
            entity.ToTable("inventory_commitments", table =>
                table.HasCheckConstraint("CK_inventory_commitments_Quantity_Positive", "\"Quantity\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => x.HoldId).IsUnique();
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.HasIndex(x => new { x.InventoryPoolId, x.CreatedAtUtc });
            entity.HasOne<InventoryPoolRecord>().WithMany().HasForeignKey(x => x.InventoryPoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<InventoryHoldRecord>().WithMany().HasForeignKey(x => x.HoldId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class InventoryCommitmentRecord
{
    public Guid Id { get; set; }
    public Guid HoldId { get; set; }
    public Guid BookingId { get; set; }
    public Guid PaymentAttemptId { get; set; }
    public Guid InventoryPoolId { get; set; }
    public required string AccountId { get; set; }
    public int Quantity { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class InventoryConfigurationRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class InventoryPoolRecord
{
    public Guid Id { get; set; }
    public Guid InventoryConfigurationId { get; set; }
    public InventoryOccupancy Occupancy { get; set; }
    public int Capacity { get; set; }
}

public sealed class InventoryAuditRecord
{
    public Guid Id { get; set; }
    public Guid InventoryConfigurationId { get; set; }
    public Guid DepartureId { get; set; }
    public required string ActorAccountId { get; set; }
    public required string CorrelationId { get; set; }
    public required string Reason { get; set; }
    public required string Action { get; set; }
    public int Version { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class InventoryHoldRecord
{
    public Guid Id { get; set; }
    public Guid InventoryPoolId { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public Guid QuoteId { get; set; }
    public required string AccountId { get; set; }
    public InventoryOccupancy Occupancy { get; set; }
    public int Quantity { get; set; }
    public InventoryHoldState State { get; set; }
    public required string IdempotencyKeyHash { get; set; }
    public required string RequestFingerprint { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? TerminalAtUtc { get; set; }
}
