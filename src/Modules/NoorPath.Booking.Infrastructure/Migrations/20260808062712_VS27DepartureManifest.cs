using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS27DepartureManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departure_manifest_audits",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreviousNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResultingNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreviousIsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    ResultingIsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    PreviousVersion = table.Column<int>(type: "integer", nullable: false),
                    ResultingVersion = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departure_manifest_audits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departure_manifest_travellers",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departure_manifest_travellers", x => x.Id);
                    table.CheckConstraint("CK_departure_manifest_travellers_Version_Positive", "\"Version\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_departure_manifest_audits_OperatorId_DepartureId_OccurredAt~",
                schema: "booking",
                table: "departure_manifest_audits",
                columns: new[] { "OperatorId", "DepartureId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_departure_manifest_audits_TravellerId_OccurredAtUtc",
                schema: "booking",
                table: "departure_manifest_audits",
                columns: new[] { "TravellerId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_departure_manifest_travellers_DepartureId_UpdatedAtUtc",
                schema: "booking",
                table: "departure_manifest_travellers",
                columns: new[] { "DepartureId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_departure_manifest_travellers_OperatorId_DepartureId_Travel~",
                schema: "booking",
                table: "departure_manifest_travellers",
                columns: new[] { "OperatorId", "DepartureId", "TravellerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "departure_manifest_audits",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "departure_manifest_travellers",
                schema: "booking");
        }
    }
}
