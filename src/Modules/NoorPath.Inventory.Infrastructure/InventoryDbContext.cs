using Microsoft.EntityFrameworkCore;
using NoorPath.Inventory;

namespace NoorPath.Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryConfigurationRecord> Configurations => Set<InventoryConfigurationRecord>();
    public DbSet<InventoryPoolRecord> Pools => Set<InventoryPoolRecord>();
    public DbSet<InventoryAuditRecord> Audits => Set<InventoryAuditRecord>();

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
    }
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
