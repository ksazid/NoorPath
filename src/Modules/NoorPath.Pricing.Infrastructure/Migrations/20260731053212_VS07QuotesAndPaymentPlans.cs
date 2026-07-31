using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Pricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS07QuotesAndPaymentPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "pricing",
                table: "pricing_audits",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent",
                schema: "pricing",
                table: "price_versions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalPaymentDueDaysBeforeDeparture",
                schema: "pricing",
                table: "price_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstalmentDayOfMonth",
                schema: "pricing",
                table: "price_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercent",
                schema: "pricing",
                table: "price_plans",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalPaymentDueDaysBeforeDeparture",
                schema: "pricing",
                table: "price_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstalmentDayOfMonth",
                schema: "pricing",
                table: "price_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "quotes",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PriceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Occupancy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TravellerCount = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueNow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remaining = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotes_price_versions_PriceVersionId",
                        column: x => x.PriceVersionId,
                        principalSchema: "pricing",
                        principalTable: "price_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_instalments",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_instalments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quote_instalments_quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "pricing",
                        principalTable: "quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quote_travellers",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_travellers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quote_travellers_quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "pricing",
                        principalTable: "quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quote_instalments_QuoteId_Sequence",
                schema: "pricing",
                table: "quote_instalments",
                columns: new[] { "QuoteId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_travellers_QuoteId_Position",
                schema: "pricing",
                table: "quote_travellers",
                columns: new[] { "QuoteId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_travellers_QuoteId_TravellerId",
                schema: "pricing",
                table: "quote_travellers",
                columns: new[] { "QuoteId", "TravellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotes_AccountId",
                schema: "pricing",
                table: "quotes",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_DepartureId",
                schema: "pricing",
                table: "quotes",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_ExpiresAtUtc",
                schema: "pricing",
                table: "quotes",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_PriceVersionId",
                schema: "pricing",
                table: "quotes",
                column: "PriceVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quote_instalments",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "quote_travellers",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "quotes",
                schema: "pricing");

            migrationBuilder.DropColumn(
                name: "DepositPercent",
                schema: "pricing",
                table: "price_versions");

            migrationBuilder.DropColumn(
                name: "FinalPaymentDueDaysBeforeDeparture",
                schema: "pricing",
                table: "price_versions");

            migrationBuilder.DropColumn(
                name: "InstalmentDayOfMonth",
                schema: "pricing",
                table: "price_versions");

            migrationBuilder.DropColumn(
                name: "DepositPercent",
                schema: "pricing",
                table: "price_plans");

            migrationBuilder.DropColumn(
                name: "FinalPaymentDueDaysBeforeDeparture",
                schema: "pricing",
                table: "price_plans");

            migrationBuilder.DropColumn(
                name: "InstalmentDayOfMonth",
                schema: "pricing",
                table: "price_plans");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "pricing",
                table: "pricing_audits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }
    }
}
