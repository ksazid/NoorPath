using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS26AccommodationAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accommodation_assignment_audits",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Stay = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreviousRoomVersion = table.Column<int>(type: "integer", nullable: false),
                    ResultingRoomVersion = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accommodation_assignment_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accommodation_assignment_audits_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accommodation_rooms",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Stay = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RoomType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accommodation_rooms", x => x.Id);
                    table.CheckConstraint("CK_accommodation_rooms_Version_Positive", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_accommodation_rooms_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accommodation_assignments",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stay = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accommodation_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accommodation_assignments_accommodation_rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "booking",
                        principalTable: "accommodation_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accommodation_assignments_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_assignment_audits_BookingId_OccurredAtUtc",
                schema: "booking",
                table: "accommodation_assignment_audits",
                columns: new[] { "BookingId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_assignment_audits_RoomId_OccurredAtUtc",
                schema: "booking",
                table: "accommodation_assignment_audits",
                columns: new[] { "RoomId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_assignments_BookingId_Stay_TravellerId",
                schema: "booking",
                table: "accommodation_assignments",
                columns: new[] { "BookingId", "Stay", "TravellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_assignments_RoomId_TravellerId",
                schema: "booking",
                table: "accommodation_assignments",
                columns: new[] { "RoomId", "TravellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_rooms_BookingId_Stay_Label",
                schema: "booking",
                table: "accommodation_rooms",
                columns: new[] { "BookingId", "Stay", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_rooms_OperatorId_BookingId_Stay",
                schema: "booking",
                table: "accommodation_rooms",
                columns: new[] { "OperatorId", "BookingId", "Stay" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accommodation_assignment_audits",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "accommodation_assignments",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "accommodation_rooms",
                schema: "booking");
        }
    }
}
