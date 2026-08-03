using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS16CancellationRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CancellationRequestId",
                schema: "booking",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAtUtc",
                schema: "booking",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cancellation_requests",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ReasonCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PolicyTimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DepartureAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DaysBeforeDeparture = table.Column<int>(type: "integer", nullable: false),
                    WindowMinimumDaysBeforeDeparture = table.Column<int>(type: "integer", nullable: false),
                    FeeBasisPoints = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SettledAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentageFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NonRefundableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundProcessingBusinessDays = table.Column<int>(type: "integer", nullable: false),
                    CalculationJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DecisionActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancellation_requests", x => x.Id);
                    table.CheckConstraint("CK_cancellation_requests_Amount_Composition", "\"SettledAmount\" = \"PercentageFee\" + \"NonRefundableAmount\" + \"RefundableAmount\"");
                    table.CheckConstraint("CK_cancellation_requests_Amounts_NonNegative", "\"SettledAmount\" >= 0 AND \"PercentageFee\" >= 0 AND \"NonRefundableAmount\" >= 0 AND \"RefundableAmount\" >= 0");
                    table.CheckConstraint("CK_cancellation_requests_FeeBasisPoints", "\"FeeBasisPoints\" >= 0 AND \"FeeBasisPoints\" <= 10000");
                    table.CheckConstraint("CK_cancellation_requests_RefundDays", "\"RefundProcessingBusinessDays\" > 0");
                    table.ForeignKey(
                        name: "FK_cancellation_requests_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cancellation_audits",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CancellationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancellation_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cancellation_audits_cancellation_requests_CancellationReque~",
                        column: x => x.CancellationRequestId,
                        principalSchema: "booking",
                        principalTable: "cancellation_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_CancellationRequestId",
                schema: "booking",
                table: "bookings",
                column: "CancellationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_audits_BookingId_OccurredAtUtc",
                schema: "booking",
                table: "cancellation_audits",
                columns: new[] { "BookingId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_audits_CancellationRequestId_OccurredAtUtc",
                schema: "booking",
                table: "cancellation_audits",
                columns: new[] { "CancellationRequestId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_requests_AccountId_BookingId_RequestedAtUtc",
                schema: "booking",
                table: "cancellation_requests",
                columns: new[] { "AccountId", "BookingId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_requests_AccountId_IdempotencyKeyHash",
                schema: "booking",
                table: "cancellation_requests",
                columns: new[] { "AccountId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_requests_BookingId",
                schema: "booking",
                table: "cancellation_requests",
                column: "BookingId",
                unique: true,
                filter: "\"State\" IN ('Requested', 'Approved', 'Applying', 'Exception')");

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_requests_OperatorId_State_UpdatedAtUtc",
                schema: "booking",
                table: "cancellation_requests",
                columns: new[] { "OperatorId", "State", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cancellation_audits",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "cancellation_requests",
                schema: "booking");

            migrationBuilder.DropIndex(
                name: "IX_bookings_CancellationRequestId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CancellationRequestId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                schema: "booking",
                table: "bookings");
        }
    }
}
