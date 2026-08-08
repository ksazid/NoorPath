using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS28DepartureHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departure_handover_audits",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreviousVersion = table.Column<int>(type: "integer", nullable: false),
                    ResultingVersion = table.Column<int>(type: "integer", nullable: false),
                    TravellerCount = table.Column<int>(type: "integer", nullable: false),
                    BlockedCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departure_handover_audits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departure_handovers",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    FinalNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedByAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departure_handovers", x => x.Id);
                    table.CheckConstraint("CK_departure_handovers_Version_Positive", "\"Version\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_departure_handover_audits_OperatorId_DepartureId_OccurredAt~",
                schema: "booking",
                table: "departure_handover_audits",
                columns: new[] { "OperatorId", "DepartureId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_departure_handovers_OperatorId_DepartureId",
                schema: "booking",
                table: "departure_handovers",
                columns: new[] { "OperatorId", "DepartureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "departure_handover_audits",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "departure_handovers",
                schema: "booking");
        }
    }
}
