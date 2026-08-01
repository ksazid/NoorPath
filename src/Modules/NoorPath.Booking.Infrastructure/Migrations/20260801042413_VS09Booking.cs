using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS09Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryHoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Occupancy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TravellerCount = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueNow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remaining = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IdempotencyKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.CheckConstraint("CK_bookings_Amounts_NonNegative", "\"UnitPrice\" >= 0 AND \"Total\" >= 0 AND \"DueNow\" >= 0 AND \"Remaining\" >= 0");
                    table.CheckConstraint("CK_bookings_Total_Composition", "\"Total\" = \"DueNow\" + \"Remaining\"");
                    table.CheckConstraint("CK_bookings_TravellerCount_Positive", "\"TravellerCount\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "booking",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateVersion = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "booking_instalments",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_instalments", x => x.Id);
                    table.CheckConstraint("CK_booking_instalments_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_booking_instalments_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_travellers",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_travellers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_booking_travellers_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_instalments_BookingId_Sequence",
                schema: "booking",
                table: "booking_instalments",
                columns: new[] { "BookingId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_travellers_BookingId_Position",
                schema: "booking",
                table: "booking_travellers",
                columns: new[] { "BookingId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_travellers_BookingId_TravellerId",
                schema: "booking",
                table: "booking_travellers",
                columns: new[] { "BookingId", "TravellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_AccountId_CreatedAtUtc",
                schema: "booking",
                table: "bookings",
                columns: new[] { "AccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_AccountId_IdempotencyKeyHash",
                schema: "booking",
                table: "bookings",
                columns: new[] { "AccountId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_InventoryHoldId",
                schema: "booking",
                table: "bookings",
                column: "InventoryHoldId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_QuoteId",
                schema: "booking",
                table: "bookings",
                column: "QuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_Reference",
                schema: "booking",
                table: "bookings",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_State_UpdatedAtUtc",
                schema: "booking",
                table: "bookings",
                columns: new[] { "State", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_State_NextAttemptAtUtc",
                schema: "booking",
                table: "outbox_messages",
                columns: new[] { "State", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_instalments",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "booking_travellers",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "booking");
        }
    }
}
