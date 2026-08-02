using Microsoft.EntityFrameworkCore;
using NoorPath.Visa;

namespace NoorPath.Visa.Infrastructure;

public sealed class VisaDbContext(DbContextOptions<VisaDbContext> options) : DbContext(options)
{
    public DbSet<VisaCaseRecord> Cases => Set<VisaCaseRecord>();
    public DbSet<VisaTransitionRecord> History => Set<VisaTransitionRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("visa");
        modelBuilder.Entity<VisaCaseRecord>(e => { e.ToTable("cases"); e.HasKey(x => x.Id); e.Property(x => x.OperatorId).HasMaxLength(80); e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); e.Property(x => x.CustomerAction).HasMaxLength(500); e.Property(x => x.Version).IsConcurrencyToken(); e.HasIndex(x => new { x.BookingId, x.TravellerId }).IsUnique(); e.HasIndex(x => new { x.OperatorId, x.Status, x.UpdatedAtUtc }); });
        modelBuilder.Entity<VisaTransitionRecord>(e => { e.ToTable("transitions"); e.HasKey(x => x.Id); e.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(32); e.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(32); e.Property(x => x.ActorId).HasMaxLength(120); e.Property(x => x.Reason).HasMaxLength(500); e.HasIndex(x => new { x.CaseId, x.OccurredAtUtc }); });
    }
}
public sealed class VisaCaseRecord { public Guid Id { get; set; } public Guid BookingId { get; set; } public Guid TravellerId { get; set; } public required string OperatorId { get; set; } public VisaStatus Status { get; set; } public string? CustomerAction { get; set; } public int Version { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset UpdatedAtUtc { get; set; } }
public sealed class VisaTransitionRecord { public Guid Id { get; set; } public Guid CaseId { get; set; } public VisaStatus PreviousStatus { get; set; } public VisaStatus NewStatus { get; set; } public required string ActorId { get; set; } public string? Reason { get; set; } public int Version { get; set; } public DateTimeOffset OccurredAtUtc { get; set; } }
