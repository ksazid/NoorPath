using Microsoft.EntityFrameworkCore;
using NoorPath.FamilyBooking;

namespace NoorPath.FamilyBooking.Infrastructure;

public sealed class FamilyBookingDbContext(DbContextOptions<FamilyBookingDbContext> options) : DbContext(options)
{
    public DbSet<FamilyPartyRecord> Parties => Set<FamilyPartyRecord>();
    public DbSet<FamilyPartyMemberRecord> Members => Set<FamilyPartyMemberRecord>();
    public DbSet<MahramLinkRecord> MahramLinks => Set<MahramLinkRecord>();
    public DbSet<FamilyBookingAuditRecord> Audit => Set<FamilyBookingAuditRecord>();
    public DbSet<FamilyQuoteSnapshotRecord> QuoteSnapshots => Set<FamilyQuoteSnapshotRecord>();
    public DbSet<FamilyBookingSnapshotRecord> BookingSnapshots => Set<FamilyBookingSnapshotRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("family_booking");

        modelBuilder.Entity<FamilyPartyRecord>(entity =>
        {
            entity.ToTable("parties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Name).HasMaxLength(FamilyBookingPolicy.MaximumPartyNameLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.PolicyVersion).HasMaxLength(40);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.AccountId, x.Status, x.UpdatedAtUtc });
        });

        modelBuilder.Entity<FamilyPartyMemberRecord>(entity =>
        {
            entity.ToTable("members");
            entity.HasKey(x => new { x.FamilyPartyId, x.TravellerId });
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.AccountId, x.TravellerId });
            entity.HasOne<FamilyPartyRecord>()
                .WithMany()
                .HasForeignKey(x => x.FamilyPartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MahramLinkRecord>(entity =>
        {
            entity.ToTable("mahram_links");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.RelationshipType).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Declaration).HasMaxLength(FamilyBookingPolicy.MaximumDeclarationLength);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.AccountId, x.FamilyPartyId });
            entity.HasIndex(x => new { x.FamilyPartyId, x.ProtectedTravellerId, x.MahramTravellerId })
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
            entity.HasOne<FamilyPartyRecord>()
                .WithMany()
                .HasForeignKey(x => x.FamilyPartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FamilyBookingAuditRecord>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.ActorId).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.SubjectType).HasMaxLength(80);
            entity.Property(x => x.DetailJson).HasColumnType("jsonb");
            entity.HasIndex(x => new { x.AccountId, x.OccurredAtUtc });
        });

        modelBuilder.Entity<FamilyQuoteSnapshotRecord>(entity =>
        {
            entity.ToTable("quote_snapshots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.PolicyVersion).HasMaxLength(40);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.QuoteId).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.FamilyPartyId });
        });

        modelBuilder.Entity<FamilyBookingSnapshotRecord>(entity =>
        {
            entity.ToTable("booking_snapshots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.PolicyVersion).HasMaxLength(40);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.FamilyPartyId });
        });
    }
}

public sealed class FamilyPartyRecord
{
    public Guid Id { get; set; }
    public required string AccountId { get; set; }
    public required string Name { get; set; }
    public FamilyPartyStatus Status { get; set; }
    public required string PolicyVersion { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class FamilyPartyMemberRecord
{
    public Guid FamilyPartyId { get; set; }
    public required string AccountId { get; set; }
    public Guid TravellerId { get; set; }
    public int Version { get; set; }
    public DateTimeOffset AddedAtUtc { get; set; }
    public DateTimeOffset? RemovedAtUtc { get; set; }
}

public sealed class MahramLinkRecord
{
    public Guid Id { get; set; }
    public Guid FamilyPartyId { get; set; }
    public required string AccountId { get; set; }
    public Guid ProtectedTravellerId { get; set; }
    public Guid MahramTravellerId { get; set; }
    public MahramRelationshipType RelationshipType { get; set; }
    public required string Declaration { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class FamilyBookingAuditRecord
{
    public Guid Id { get; set; }
    public required string AccountId { get; set; }
    public required string ActorId { get; set; }
    public required string Action { get; set; }
    public required string SubjectType { get; set; }
    public Guid SubjectId { get; set; }
    public required string DetailJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class FamilyQuoteSnapshotRecord
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid FamilyPartyId { get; set; }
    public required string AccountId { get; set; }
    public required string PolicyVersion { get; set; }
    public int PartyVersion { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class FamilyBookingSnapshotRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid FamilyPartyId { get; set; }
    public required string AccountId { get; set; }
    public required string PolicyVersion { get; set; }
    public int PartyVersion { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
