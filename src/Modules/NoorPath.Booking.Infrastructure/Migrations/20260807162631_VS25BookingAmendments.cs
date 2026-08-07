using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS25BookingAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "booking",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "booking_amendments",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreviousBookingVersion = table.Column<int>(type: "integer", nullable: false),
                    ResultingBookingVersion = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PriceDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviewFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BeforeSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    AfterSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_amendments", x => x.Id);
                    table.CheckConstraint("CK_booking_amendments_Version_Progression", "\"ResultingBookingVersion\" = \"PreviousBookingVersion\" + 1");
                    table.ForeignKey(
                        name: "FK_booking_amendments_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_Version_Positive",
                schema: "booking",
                table: "bookings",
                sql: "\"Version\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_booking_amendments_BookingId_OccurredAtUtc",
                schema: "booking",
                table: "booking_amendments",
                columns: new[] { "BookingId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_amendments_BookingId_ResultingBookingVersion",
                schema: "booking",
                table: "booking_amendments",
                columns: new[] { "BookingId", "ResultingBookingVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_amendments",
                schema: "booking");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_Version_Positive",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "booking",
                table: "bookings");
        }
    }
}
