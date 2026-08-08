using Microsoft.EntityFrameworkCore;

namespace NoorPath.Booking.Infrastructure;

public static class DepartureManifestPersistence
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartureManifestTravellerRecord>(entity =>
        {
            entity.ToTable("departure_manifest_travellers", "booking", table =>
                table.HasCheckConstraint(
                    "CK_departure_manifest_travellers_Version_Positive",
                    "\"Version\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OperatorId, x.DepartureId, x.TravellerId }).IsUnique();
            entity.HasIndex(x => new { x.DepartureId, x.UpdatedAtUtc });
        });

        modelBuilder.Entity<DepartureManifestAuditRecord>(entity =>
        {
            entity.ToTable("departure_manifest_audits", "booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(32);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.OperatorId, x.DepartureId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.TravellerId, x.OccurredAtUtc });
        });
    }
}

public sealed class DepartureManifestTravellerRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public Guid BookingId { get; set; }
    public Guid TravellerId { get; set; }
    public required string OperatorId { get; set; }
    public required string ActorAccountId { get; set; }
    public string? Note { get; set; }
    public bool IsAcknowledged { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class DepartureManifestAuditRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public Guid BookingId { get; set; }
    public Guid TravellerId { get; set; }
    public required string OperatorId { get; set; }
    public required string ActorAccountId { get; set; }
    public required string Action { get; set; }
    public string? Note { get; set; }
    public int PreviousVersion { get; set; }
    public int ResultingVersion { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
