using Microsoft.EntityFrameworkCore;

namespace NoorPath.Traveller.Infrastructure;

public sealed class TravellerDbContext(DbContextOptions<TravellerDbContext> options) : DbContext(options)
{
    public DbSet<TravellerRecord> Travellers => Set<TravellerRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("traveller");

        modelBuilder.Entity<TravellerRecord>(entity =>
        {
            entity.ToTable("travellers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerAccountId).HasMaxLength(120);
            entity.Property(x => x.FullName).HasMaxLength(120);
            entity.HasIndex(x => x.OwnerAccountId);
        });
    }
}

public sealed class TravellerRecord
{
    public Guid Id { get; set; }
    public required string OwnerAccountId { get; set; }
    public required string FullName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
