using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;

namespace NoorPath.Booking.Infrastructure;

public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options)
    : DbContext(options)
{
    public DbSet<BookingRecord> Bookings => Set<BookingRecord>();
    public DbSet<BookingTravellerRecord> Travellers => Set<BookingTravellerRecord>();
    public DbSet<BookingInstalmentRecord> Instalments => Set<BookingInstalmentRecord>();
    public DbSet<BookingOutboxRecord> OutboxMessages => Set<BookingOutboxRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");

        modelBuilder.Entity<BookingRecord>(entity =>
        {
            entity.ToTable("bookings", table =>
            {
                table.HasCheckConstraint(
                    "CK_bookings_TravellerCount_Positive",
                    "\"TravellerCount\" > 0");
                table.HasCheckConstraint(
                    "CK_bookings_Amounts_NonNegative",
                    "\"UnitPrice\" >= 0 AND \"Total\" >= 0 AND \"DueNow\" >= 0 AND \"Remaining\" >= 0");
                table.HasCheckConstraint(
                    "CK_bookings_Total_Composition",
                    "\"Total\" = \"DueNow\" + \"Remaining\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reference).HasMaxLength(24);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.Occupancy).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.Property(x => x.DueNow).HasPrecision(18, 2);
            entity.Property(x => x.Remaining).HasPrecision(18, 2);
            entity.Property(x => x.State).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.IdempotencyKeyHash).HasMaxLength(64);
            entity.Property(x => x.RequestFingerprint).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => x.Reference).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.IdempotencyKeyHash }).IsUnique();
            entity.HasIndex(x => x.InventoryHoldId).IsUnique();
            entity.HasIndex(x => x.QuoteId).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.State, x.UpdatedAtUtc });
        });

        modelBuilder.Entity<BookingTravellerRecord>(entity =>
        {
            entity.ToTable("booking_travellers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(120);
            entity.HasIndex(x => new { x.BookingId, x.Position }).IsUnique();
            entity.HasIndex(x => new { x.BookingId, x.TravellerId }).IsUnique();
            entity.HasOne<BookingRecord>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingInstalmentRecord>(entity =>
        {
            entity.ToTable("booking_instalments", table =>
                table.HasCheckConstraint(
                    "CK_booking_instalments_Amount_Positive",
                    "\"Amount\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.BookingId, x.Sequence }).IsUnique();
            entity.HasOne<BookingRecord>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingOutboxRecord>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).HasMaxLength(80);
            entity.Property(x => x.AggregateType).HasMaxLength(40);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.CausationId).HasMaxLength(100);
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.Property(x => x.State).HasMaxLength(20);
            entity.HasIndex(x => new { x.State, x.NextAttemptAtUtc });
        });
    }
}

public sealed class BookingRecord
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public required string AccountId { get; set; }
    public required string OperatorId { get; set; }
    public Guid DepartureId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid PriceVersionId { get; set; }
    public Guid InventoryHoldId { get; set; }
    public BookingOccupancy Occupancy { get; set; }
    public int TravellerCount { get; set; }
    public required string Currency { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public decimal DueNow { get; set; }
    public decimal Remaining { get; set; }
    public BookingState State { get; set; }
    public required string IdempotencyKeyHash { get; set; }
    public required string RequestFingerprint { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class BookingTravellerRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid TravellerId { get; set; }
    public int Position { get; set; }
    public required string FullName { get; set; }
    public DateOnly DateOfBirth { get; set; }
}

public sealed class BookingInstalmentRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public int Sequence { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
}

public sealed class BookingOutboxRecord
{
    public Guid EventId { get; set; }
    public required string EventType { get; set; }
    public int EventVersion { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public required string AggregateType { get; set; }
    public Guid AggregateId { get; set; }
    public int AggregateVersion { get; set; }
    public required string CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public required string Payload { get; set; }
    public required string State { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
