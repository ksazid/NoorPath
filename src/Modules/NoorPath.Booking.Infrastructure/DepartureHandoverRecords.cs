using Microsoft.EntityFrameworkCore;

namespace NoorPath.Booking.Infrastructure;

public static class DepartureHandoverPersistence
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartureHandoverRecord>(entity =>
        {
            entity.ToTable("departure_handovers", "booking", table =>
                table.HasCheckConstraint(
                    "CK_departure_handovers_Version_Positive",
                    "\"Version\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.CompletedByAccountId).HasMaxLength(120);
            entity.Property(x => x.FinalNote).HasMaxLength(500);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OperatorId, x.DepartureId }).IsUnique();
        });

        modelBuilder.Entity<DepartureHandoverAuditRecord>(entity =>
        {
            entity.ToTable("departure_handover_audits", "booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(32);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.OperatorId, x.DepartureId, x.OccurredAtUtc });
        });
    }
}

public sealed class DepartureHandoverRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public bool IsCompleted { get; set; }
    public string? FinalNote { get; set; }
    public string? CompletedByAccountId { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class DepartureHandoverAuditRecord
{
    public Guid Id { get; set; }
    public Guid DepartureId { get; set; }
    public required string OperatorId { get; set; }
    public required string ActorAccountId { get; set; }
    public required string Action { get; set; }
    public required string Note { get; set; }
    public int PreviousVersion { get; set; }
    public int ResultingVersion { get; set; }
    public int TravellerCount { get; set; }
    public int BlockedCount { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
