using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;

namespace NoorPath.Booking.Infrastructure;

public static class AccommodationPersistence
{
    public static void ConfigureAccommodation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccommodationRoomRecord>(entity =>
        {
            entity.ToTable("accommodation_rooms", "booking", table =>
            {
                table.HasCheckConstraint(
                    "CK_accommodation_rooms_Version_Positive",
                    "\"Version\" > 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Stay).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.RoomType).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Label).HasMaxLength(80);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.BookingId, x.Stay, x.Label }).IsUnique();
            entity.HasIndex(x => new { x.OperatorId, x.BookingId, x.Stay });
            entity.HasOne<BookingRecord>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccommodationAssignmentRecord>(entity =>
        {
            entity.ToTable("accommodation_assignments", "booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Stay).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => new { x.BookingId, x.Stay, x.TravellerId }).IsUnique();
            entity.HasIndex(x => new { x.RoomId, x.TravellerId }).IsUnique();
            entity.HasOne<AccommodationRoomRecord>()
                .WithMany()
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BookingRecord>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccommodationAssignmentAuditRecord>(entity =>
        {
            entity.ToTable("accommodation_assignment_audits", "booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.Stay).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.BookingId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.RoomId, x.OccurredAtUtc });
            entity.HasOne<BookingRecord>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        DepartureManifestPersistence.Configure(modelBuilder);
        DepartureHandoverPersistence.Configure(modelBuilder);
    }
}

public sealed class AccommodationRoomRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string OperatorId { get; set; }
    public AccommodationStay Stay { get; set; }
    public AccommodationRoomType RoomType { get; set; }
    public required string Label { get; set; }
    public int Version { get; set; } = 1;
    public bool IsLocked { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class AccommodationAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string OperatorId { get; set; }
    public Guid RoomId { get; set; }
    public Guid TravellerId { get; set; }
    public AccommodationStay Stay { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
}

public sealed class AccommodationAssignmentAuditRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string OperatorId { get; set; }
    public required string ActorAccountId { get; set; }
    public Guid? TravellerId { get; set; }
    public Guid? PreviousRoomId { get; set; }
    public Guid? RoomId { get; set; }
    public AccommodationStay Stay { get; set; }
    public required string Action { get; set; }
    public required string Reason { get; set; }
    public int PreviousRoomVersion { get; set; }
    public int ResultingRoomVersion { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
