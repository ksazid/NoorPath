using Microsoft.EntityFrameworkCore;
using NoorPath.Documents;

namespace NoorPath.Documents.Infrastructure;

public sealed class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : DbContext(options)
{
    public DbSet<DocumentRequirementRecord> Requirements => Set<DocumentRequirementRecord>();
    public DbSet<DocumentSubmissionRecord> Submissions => Set<DocumentSubmissionRecord>();
    public DbSet<DocumentAuditRecord> Audit => Set<DocumentAuditRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documents");
        modelBuilder.Entity<DocumentRequirementRecord>(e => { e.ToTable("requirements"); e.HasKey(x => x.Id); e.Property(x => x.PolicyVersion).HasMaxLength(16); e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32); e.HasIndex(x => new { x.BookingId, x.TravellerId, x.Kind }).IsUnique(); });
        modelBuilder.Entity<DocumentSubmissionRecord>(e => { e.ToTable("submissions"); e.HasKey(x => x.Id); e.Property(x => x.ObjectKey).HasMaxLength(160); e.Property(x => x.DeclaredContentType).HasMaxLength(40); e.Property(x => x.State).HasConversion<string>().HasMaxLength(32); e.Property(x => x.MalwareStatus).HasConversion<string>().HasMaxLength(24); e.Property(x => x.ReviewReason).HasMaxLength(500); e.Property(x => x.Version).IsConcurrencyToken(); e.HasIndex(x => x.ObjectKey).IsUnique(); e.HasIndex(x => new { x.RequirementId, x.CreatedAtUtc }); });
        modelBuilder.Entity<DocumentAuditRecord>(e => { e.ToTable("audit"); e.HasKey(x => x.Id); e.Property(x => x.Action).HasMaxLength(48); e.Property(x => x.ActorId).HasMaxLength(120); e.Property(x => x.Purpose).HasMaxLength(500); e.HasIndex(x => new { x.SubmissionId, x.OccurredAtUtc }); });
    }
}
public sealed class DocumentRequirementRecord { public Guid Id { get; set; } public Guid BookingId { get; set; } public Guid TravellerId { get; set; } public string PolicyVersion { get; set; } = "v1"; public DocumentKind Kind { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } }
public sealed class DocumentSubmissionRecord { public Guid Id { get; set; } public Guid RequirementId { get; set; } public string ObjectKey { get; set; } = ""; public string DeclaredContentType { get; set; } = ""; public long DeclaredSize { get; set; } public SubmissionState State { get; set; } public MalwareStatus MalwareStatus { get; set; } public string? ReviewReason { get; set; } public string? ReviewedBy { get; set; } public DateTimeOffset? ReviewedAtUtc { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset? DeleteAfterUtc { get; set; } public DateTimeOffset? HoldAtUtc { get; set; } public int Version { get; set; } }
public sealed class DocumentAuditRecord { public Guid Id { get; set; } public Guid SubmissionId { get; set; } public string Action { get; set; } = ""; public string ActorId { get; set; } = ""; public string Purpose { get; set; } = ""; public DateTimeOffset OccurredAtUtc { get; set; } }
