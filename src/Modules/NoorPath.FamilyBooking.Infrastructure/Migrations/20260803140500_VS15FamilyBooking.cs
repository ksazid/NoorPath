using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace NoorPath.FamilyBooking.Infrastructure.Migrations;

[DbContext(typeof(FamilyBookingDbContext))]
[Migration("20260803140500_VS15FamilyBooking")]
public sealed class VS15FamilyBooking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "family_booking");

        migrationBuilder.CreateTable(
            name: "parties",
            schema: "family_booking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                PolicyVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_parties", x => x.Id));

        migrationBuilder.CreateTable(
            name: "audit_events",
            schema: "family_booking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ActorId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SubjectType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_audit_events", x => x.Id));

        migrationBuilder.CreateTable(
            name: "quote_snapshots",
            schema: "family_booking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                FamilyPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                PolicyVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                PartyVersion = table.Column<int>(type: "integer", nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_quote_snapshots", x => x.Id));

        migrationBuilder.CreateTable(
            name: "booking_snapshots",
            schema: "family_booking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                FamilyPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                PolicyVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                PartyVersion = table.Column<int>(type: "integer", nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_booking_snapshots", x => x.Id));

        migrationBuilder.CreateTable(
            name: "members",
            schema: "family_booking",
            columns: table => new
            {
                FamilyPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                AddedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RemovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_members", x => new { x.FamilyPartyId, x.TravellerId });
                table.ForeignKey("FK_members_parties_FamilyPartyId", x => x.FamilyPartyId, "family_booking", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "mahram_links",
            schema: "family_booking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FamilyPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ProtectedTravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                MahramTravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                RelationshipType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Declaration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mahram_links", x => x.Id);
                table.ForeignKey("FK_mahram_links_parties_FamilyPartyId", x => x.FamilyPartyId, "family_booking", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_parties_AccountId_Status_UpdatedAtUtc", "family_booking", "parties", new[] { "AccountId", "Status", "UpdatedAtUtc" });
        migrationBuilder.CreateIndex("IX_members_AccountId_TravellerId", "family_booking", "members", new[] { "AccountId", "TravellerId" });
        migrationBuilder.CreateIndex("IX_mahram_links_AccountId_FamilyPartyId", "family_booking", "mahram_links", new[] { "AccountId", "FamilyPartyId" });
        migrationBuilder.CreateIndex("IX_mahram_links_FamilyPartyId_ProtectedTravellerId_MahramTravellerId", "family_booking", "mahram_links", new[] { "FamilyPartyId", "ProtectedTravellerId", "MahramTravellerId" }, unique: true, filter: "\"IsActive\" = TRUE");
        migrationBuilder.CreateIndex("IX_audit_events_AccountId_OccurredAtUtc", "family_booking", "audit_events", new[] { "AccountId", "OccurredAtUtc" });
        migrationBuilder.CreateIndex("IX_quote_snapshots_QuoteId", "family_booking", "quote_snapshots", "QuoteId", unique: true);
        migrationBuilder.CreateIndex("IX_quote_snapshots_AccountId_FamilyPartyId", "family_booking", "quote_snapshots", new[] { "AccountId", "FamilyPartyId" });
        migrationBuilder.CreateIndex("IX_booking_snapshots_BookingId", "family_booking", "booking_snapshots", "BookingId", unique: true);
        migrationBuilder.CreateIndex("IX_booking_snapshots_AccountId_FamilyPartyId", "family_booking", "booking_snapshots", new[] { "AccountId", "FamilyPartyId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("audit_events", "family_booking");
        migrationBuilder.DropTable("booking_snapshots", "family_booking");
        migrationBuilder.DropTable("quote_snapshots", "family_booking");
        migrationBuilder.DropTable("mahram_links", "family_booking");
        migrationBuilder.DropTable("members", "family_booking");
        migrationBuilder.DropTable("parties", "family_booking");
    }
}
