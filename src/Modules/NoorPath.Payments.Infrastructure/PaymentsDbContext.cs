using Microsoft.EntityFrameworkCore;
using NoorPath.Payments;

namespace NoorPath.Payments.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentAttemptRecord> PaymentAttempts => Set<PaymentAttemptRecord>();
    public DbSet<PaymentProviderEventRecord> ProviderEvents => Set<PaymentProviderEventRecord>();
    public DbSet<RefundRecord> Refunds => Set<RefundRecord>();
    public DbSet<RefundAuditRecord> RefundAudits => Set<RefundAuditRecord>();
    public DbSet<PaymentOutboxRecord> OutboxMessages => Set<PaymentOutboxRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.Entity<PaymentAttemptRecord>(entity =>
        {
            entity.ToTable("payment_attempts", table =>
                table.HasCheckConstraint(
                    "CK_payment_attempts_Amount_Positive",
                    "\"Amount\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.State).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.Provider).HasMaxLength(40);
            entity.Property(x => x.ProviderSessionId).HasMaxLength(160);
            entity.Property(x => x.ProviderPaymentId).HasMaxLength(160);
            entity.Property(x => x.IdempotencyKeyHash).HasMaxLength(64);
            entity.Property(x => x.RequestFingerprint).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.FailureCode).HasMaxLength(80);
            entity.HasIndex(x => new { x.AccountId, x.IdempotencyKeyHash }).IsUnique();
            entity.HasIndex(x => x.ProviderSessionId).IsUnique();
            entity.HasIndex(x => x.ProviderPaymentId).IsUnique();
            entity.HasIndex(x => new { x.BookingId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.State, x.UpdatedAtUtc });
        });

        modelBuilder.Entity<PaymentProviderEventRecord>(entity =>
        {
            entity.ToTable("provider_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(40);
            entity.Property(x => x.ProviderEventId).HasMaxLength(160);
            entity.Property(x => x.EventType).HasMaxLength(80);
            entity.Property(x => x.PayloadHash).HasMaxLength(64);
            entity.Property(x => x.SignatureKeyId).HasMaxLength(80);
            entity.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique();
            entity.HasIndex(x => new { x.PaymentAttemptId, x.ReceivedAtUtc });
            entity.HasOne<PaymentAttemptRecord>()
                .WithMany()
                .HasForeignKey(x => x.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefundRecord>(entity =>
        {
            entity.ToTable("refunds", table =>
            {
                table.HasCheckConstraint(
                    "CK_refunds_Amounts_NonNegative",
                    "\"Amount\" >= 0 AND \"RefundedAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_refunds_Refunded_Not_Above_Entitlement",
                    "\"RefundedAmount\" <= \"Amount\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.RefundedAmount).HasPrecision(18, 2);
            entity.Property(x => x.State).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.Provider).HasMaxLength(40);
            entity.Property(x => x.ProviderRefundId).HasMaxLength(160);
            entity.Property(x => x.IdempotencyKeyHash).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.FailureCode).HasMaxLength(80);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.CancellationRequestId).IsUnique();
            entity.HasIndex(x => new { x.BookingId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.AccountId, x.IdempotencyKeyHash }).IsUnique();
            entity.HasIndex(x => new { x.State, x.UpdatedAtUtc });
            entity.HasIndex(x => new { x.Provider, x.ProviderRefundId }).IsUnique();
            entity.HasOne<PaymentAttemptRecord>()
                .WithMany()
                .HasForeignKey(x => x.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefundAuditRecord>(entity =>
        {
            entity.ToTable("refund_audits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.ActorAccountId).HasMaxLength(120);
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.DetailJson).HasColumnType("jsonb");
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.RefundId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.BookingId, x.OccurredAtUtc });
            entity.HasOne<RefundRecord>()
                .WithMany()
                .HasForeignKey(x => x.RefundId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentOutboxRecord>(entity =>
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

public sealed class PaymentAttemptRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string AccountId { get; set; }
    public required string Currency { get; set; }
    public decimal Amount { get; set; }
    public PaymentAttemptState State { get; set; }
    public required string Provider { get; set; }
    public required string ProviderSessionId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public required string IdempotencyKeyHash { get; set; }
    public required string RequestFingerprint { get; set; }
    public required string CorrelationId { get; set; }
    public string? FailureCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CheckoutExpiresAtUtc { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }
}

public sealed class PaymentProviderEventRecord
{
    public Guid Id { get; set; }
    public Guid PaymentAttemptId { get; set; }
    public required string Provider { get; set; }
    public required string ProviderEventId { get; set; }
    public required string EventType { get; set; }
    public required string PayloadHash { get; set; }
    public required string SignatureKeyId { get; set; }
    public ProviderEventOutcome Outcome { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}

public sealed class RefundRecord
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid CancellationRequestId { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    public required string AccountId { get; set; }
    public required string Currency { get; set; }
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public RefundState State { get; set; }
    public required string Provider { get; set; }
    public string? ProviderRefundId { get; set; }
    public required string IdempotencyKeyHash { get; set; }
    public required string CorrelationId { get; set; }
    public string? FailureCode { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }
}

public sealed class RefundAuditRecord
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public Guid BookingId { get; set; }
    public Guid CancellationRequestId { get; set; }
    public required string AccountId { get; set; }
    public required string ActorAccountId { get; set; }
    public required string Action { get; set; }
    public string? Reason { get; set; }
    public required string DetailJson { get; set; }
    public required string CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class PaymentOutboxRecord
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
