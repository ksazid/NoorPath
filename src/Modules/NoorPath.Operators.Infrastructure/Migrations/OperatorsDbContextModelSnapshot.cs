using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NoorPath.Operators;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoorPath.Operators.Infrastructure.Migrations;

[DbContext(typeof(OperatorsDbContext))]
partial class OperatorsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("operators")
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
        modelBuilder.Entity("NoorPath.Operators.Infrastructure.OperatorRecord", b =>
        {
            b.Property<string>("Id").HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<OperatorState>("State").IsRequired().HasMaxLength(24).HasColumnType("character varying(24)");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<int>("Version").IsConcurrencyToken().HasColumnType("integer");
            b.HasKey("Id");
            b.ToTable("operators", "operators");
        });
        modelBuilder.Entity("NoorPath.Operators.Infrastructure.OperatorMembershipRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<string>("AccountId").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("OperatorId").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<MembershipStatus>("Status").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("AccountId");
            b.HasIndex("OperatorId", "AccountId").IsUnique();
            b.ToTable("operator_memberships", "operators");
        });
        modelBuilder.Entity("NoorPath.Operators.Infrastructure.OperatorMembershipPermissionRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<Guid>("MembershipId").HasColumnType("uuid");
            b.Property<string>("Permission").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.HasKey("Id");
            b.HasIndex("MembershipId", "Permission").IsUnique();
            b.ToTable("operator_membership_permissions", "operators");
        });
        modelBuilder.Entity("NoorPath.Operators.Infrastructure.OperatorMembershipRecord", b =>
        {
            b.HasOne("NoorPath.Operators.Infrastructure.OperatorRecord", null).WithMany().HasForeignKey("OperatorId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
        modelBuilder.Entity("NoorPath.Operators.Infrastructure.OperatorMembershipPermissionRecord", b =>
        {
            b.HasOne("NoorPath.Operators.Infrastructure.OperatorMembershipRecord", null).WithMany().HasForeignKey("MembershipId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
    }
}
