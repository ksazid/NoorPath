using Microsoft.EntityFrameworkCore;
using NoorPath.BuildingBlocks;
using NoorPath.Operators;

namespace NoorPath.Operators.Infrastructure;

public sealed class OperatorsDbContext(DbContextOptions<OperatorsDbContext> options) : DbContext(options), IOperatorAccess, IOperatorPublicationEligibility
{
    public DbSet<OperatorRecord> Operators => Set<OperatorRecord>();
    public DbSet<OperatorMembershipRecord> Memberships => Set<OperatorMembershipRecord>();
    public DbSet<OperatorMembershipPermissionRecord> MembershipPermissions => Set<OperatorMembershipPermissionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("operators");
        modelBuilder.Entity<OperatorRecord>(entity =>
        {
            entity.ToTable("operators");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(80);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.State).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });
        modelBuilder.Entity<OperatorMembershipRecord>(entity =>
        {
            entity.ToTable("operator_memberships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(80);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => new { x.OperatorId, x.AccountId }).IsUnique();
            entity.HasIndex(x => x.AccountId);
            entity.HasOne<OperatorRecord>().WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OperatorMembershipPermissionRecord>(entity =>
        {
            entity.ToTable("operator_membership_permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Permission).HasMaxLength(100);
            entity.HasIndex(x => new { x.MembershipId, x.Permission }).IsUnique();
            entity.HasOne<OperatorMembershipRecord>().WithMany().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public async Task<OperatorAccess?> FindActiveMembershipAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var membership = await (
            from member in Memberships.AsNoTracking()
            join operation in Operators.AsNoTracking() on member.OperatorId equals operation.Id
            where member.AccountId == accountId.Value && member.Status == MembershipStatus.Active
            orderby member.Id
            select new { Member = member, Operator = operation }).FirstOrDefaultAsync(cancellationToken);

        if (membership is null) return null;

        var permissions = await MembershipPermissions.AsNoTracking()
            .Where(x => x.MembershipId == membership.Member.Id)
            .Select(x => x.Permission)
            .ToHashSetAsync(cancellationToken);

        return new(membership.Operator.Id, membership.Operator.DisplayName, membership.Operator.State, permissions);
    }

    public async Task<OperatorPublicationEligibility?> FindPublicationEligibilityAsync(
        string operatorId,
        CancellationToken cancellationToken)
    {
        var operation = await Operators.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operatorId, cancellationToken);

        return operation is null
            ? null
            : new(
                operation.Id,
                operation.State,
                operation.State == OperatorState.Approved);
    }
}

public sealed class OperatorRecord
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public OperatorState State { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OperatorMembershipRecord
{
    public Guid Id { get; set; }
    public required string OperatorId { get; set; }
    public required string AccountId { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OperatorMembershipPermissionRecord
{
    public Guid Id { get; set; }
    public Guid MembershipId { get; set; }
    public required string Permission { get; set; }
}
