using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NoorPath.FamilyBooking;

#nullable disable
namespace NoorPath.FamilyBooking.Infrastructure.Migrations;

public partial class VS15FamilyBooking
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.10").HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
        modelBuilder.HasDefaultSchema("family_booking");

        modelBuilder.Entity<FamilyPartyRecord>(entity =>
        {
            entity.ToTable("parties", "family_booking");
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
            entity.ToTable("members", "family_booking");
            entity.HasKey(x => new { x.FamilyPartyId, x.TravellerId });
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.AccountId, x.TravellerId });
            entity.HasOne<FamilyPartyRecord>().WithMany().HasForeignKey(x => x.FamilyPartyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MahramLinkRecord>(entity =>
        {
            entity.ToTable("mahram_links", "family_booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.RelationshipType).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Declaration).HasMaxLength(FamilyBookingPolicy.MaximumDeclarationLength);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.AccountId, x.FamilyPartyId });
            entity.HasIndex(x => new { x.FamilyPartyId, x.ProtectedTravellerId, x.MahramTravellerId }).IsUnique().HasFilter("\"IsActive\" = TRUE");
            entity.HasOne<FamilyPartyRecord>().WithMany().HasForeignKey(x => x.FamilyPartyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FamilyBookingAuditRecord>(entity =>
        {
            entity.ToTable("audit_events", "family_booking");
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
            entity.ToTable("quote_snapshots", "family_booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.PolicyVersion).HasMaxLength(40);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.QuoteId).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.FamilyPartyId });
        });

        modelBuilder.Entity<FamilyBookingSnapshotRecord>(entity =>
        {
            entity.ToTable("booking_snapshots", "family_booking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountId).HasMaxLength(120);
            entity.Property(x => x.PolicyVersion).HasMaxLength(40);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.FamilyPartyId });
        });
    }
}
