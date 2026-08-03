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
                table.ForeignKey(
                    name: "FK_members_parties_FamilyPartyId",
                    column: x => x.FamilyPartyId,
                    principalSchema: "family_booking",
                    principalTable: "parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
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
                table.ForeignKey(
                    name: "FK_mahram_links_parties_FamilyPartyId",
                    column: x => x.FamilyPartyId,
                    principalSchema: "family_booking",
                    principalTable: "parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_parties_AccountId_Status_UpdatedAtUtc",
            schema: "family_booking",
            table: "parties",
            columns: new[] { "AccountId", "Status", "UpdatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_members_AccountId_TravellerId",
            schema: "family_booking",
            table: "members",
            columns: new[] { "AccountId", "TravellerId" });
        migrationBuilder.CreateIndex(
            name: "IX_mahram_links_AccountId_FamilyPartyId",
            schema: "family_booking",
            table: "mahram_links",
            columns: new[] { "AccountId", "FamilyPartyId" });
        migrationBuilder.CreateIndex(
            name: "IX_mahram_links_FamilyPartyId_ProtectedTravellerId_MahramTravellerId",
            schema: "family_booking",
            table: "mahram_links",
            columns: new[] { "FamilyPartyId", "ProtectedTravellerId", "MahramTravellerId" },
            unique: true,
            filter: "\"IsActive\" = TRUE");
        migrationBuilder.CreateIndex(
            name: "IX_audit_events_AccountId_OccurredAtUtc",
            schema: "family_booking",
            table: "audit_events",
            columns: new[] { "AccountId", "OccurredAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_quote_snapshots_QuoteId",
            schema: "family_booking",
            table: "quote_snapshots",
            column: "QuoteId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_quote_snapshots_AccountId_FamilyPartyId",
            schema: "family_booking",
            table: "quote_snapshots",
            columns: new[] { "AccountId", "FamilyPartyId" });
        migrationBuilder.CreateIndex(
            name: "IX_booking_snapshots_BookingId",
            schema: "family_booking",
            table: "booking_snapshots",
            column: "BookingId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_booking_snapshots_AccountId_FamilyPartyId",
            schema: "family_booking",
            table: "booking_snapshots",
            columns: new[] { "AccountId", "FamilyPartyId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_events", schema: "family_booking");
        migrationBuilder.DropTable(name: "booking_snapshots", schema: "family_booking");
        migrationBuilder.DropTable(name: "quote_snapshots", schema: "family_booking");
        migrationBuilder.DropTable(name: "mahram_links", schema: "family_booking");
        migrationBuilder.DropTable(name: "members", schema: "family_booking");
        migrationBuilder.DropTable(name: "parties", schema: "family_booking");
    }
}
