using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS16CancellationRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "refunds",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancellationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProviderRefundId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IdempotencyKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SettledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.Id);
                    table.CheckConstraint("CK_refunds_Amounts_NonNegative", "\"Amount\" >= 0 AND \"RefundedAmount\" >= 0");
                    table.CheckConstraint("CK_refunds_Refunded_Not_Above_Entitlement", "\"RefundedAmount\" <= \"Amount\"");
                    table.ForeignKey(
                        name: "FK_refunds_payment_attempts_PaymentAttemptId",
                        column: x => x.PaymentAttemptId,
                        principalSchema: "payments",
                        principalTable: "payment_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refund_audits",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancellationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_refund_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refund_audits_refunds_RefundId",
                        column: x => x.RefundId,
                        principalSchema: "payments",
                        principalTable: "refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refund_audits_BookingId_OccurredAtUtc",
                schema: "payments",
                table: "refund_audits",
                columns: new[] { "BookingId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_refund_audits_RefundId_OccurredAtUtc",
                schema: "payments",
                table: "refund_audits",
                columns: new[] { "RefundId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_AccountId_IdempotencyKeyHash",
                schema: "payments",
                table: "refunds",
                columns: new[] { "AccountId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refunds_BookingId_CreatedAtUtc",
                schema: "payments",
                table: "refunds",
                columns: new[] { "BookingId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_CancellationRequestId",
                schema: "payments",
                table: "refunds",
                column: "CancellationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refunds_PaymentAttemptId",
                schema: "payments",
                table: "refunds",
                column: "PaymentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_Provider_ProviderRefundId",
                schema: "payments",
                table: "refunds",
                columns: new[] { "Provider", "ProviderRefundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refunds_State_UpdatedAtUtc",
                schema: "payments",
                table: "refunds",
                columns: new[] { "State", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refund_audits",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "refunds",
                schema: "payments");
        }
    }
}
