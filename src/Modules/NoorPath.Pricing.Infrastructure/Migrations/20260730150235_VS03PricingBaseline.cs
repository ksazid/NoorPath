using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Pricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS03PricingBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.CreateTable(
                name: "price_plans",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "occupancy_prices",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Occupancy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_occupancy_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_occupancy_prices_price_plans_PricePlanId",
                        column: x => x.PricePlanId,
                        principalSchema: "pricing",
                        principalTable: "price_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_audits",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_audits_price_plans_PricePlanId",
                        column: x => x.PricePlanId,
                        principalSchema: "pricing",
                        principalTable: "price_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_occupancy_prices_PricePlanId_Occupancy",
                schema: "pricing",
                table: "occupancy_prices",
                columns: new[] { "PricePlanId", "Occupancy" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_plans_DepartureId",
                schema: "pricing",
                table: "price_plans",
                column: "DepartureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_plans_OperatorId",
                schema: "pricing",
                table: "price_plans",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_audits_DepartureId_Version",
                schema: "pricing",
                table: "pricing_audits",
                columns: new[] { "DepartureId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_audits_PricePlanId",
                schema: "pricing",
                table: "pricing_audits",
                column: "PricePlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "occupancy_prices",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "pricing_audits",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "price_plans",
                schema: "pricing");
        }
    }
}
