using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Pricing.Infrastructure.Migrations;

[DbContext(typeof(PricingDbContext))]
[Migration("20260731001001_VS04PublishedPriceVersions")]
public partial class VS04PublishedPriceVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "price_versions",
            schema: "pricing",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                PricePlanId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                DepartureId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                OperatorId = table.Column<string>(
                    type: "character varying(80)",
                    maxLength: 80,
                    nullable: false),
                SourcePlanVersion = table.Column<int>(
                    type: "integer",
                    nullable: false),
                Currency = table.Column<string>(
                    type: "character varying(3)",
                    maxLength: 3,
                    nullable: false),
                PublishedByAccountId = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                PublishedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_price_versions", item => item.Id);
                table.ForeignKey(
                    name: "FK_price_versions_price_plans_PricePlanId",
                    column: item => item.PricePlanId,
                    principalSchema: "pricing",
                    principalTable: "price_plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "published_occupancy_prices",
            schema: "pricing",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                PriceVersionId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Occupancy = table.Column<string>(
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false),
                Amount = table.Column<decimal>(
                    type: "numeric(18,2)",
                    precision: 18,
                    scale: 2,
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_published_occupancy_prices",
                    item => item.Id);
                table.ForeignKey(
                    name:
                        "FK_published_occupancy_prices_price_versions_PriceVersionId",
                    column: item => item.PriceVersionId,
                    principalSchema: "pricing",
                    principalTable: "price_versions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_price_versions_DepartureId",
            schema: "pricing",
            table: "price_versions",
            column: "DepartureId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_price_versions_PricePlanId_SourcePlanVersion",
            schema: "pricing",
            table: "price_versions",
            columns: new[] { "PricePlanId", "SourcePlanVersion" },
            unique: true);

        migrationBuilder.CreateIndex(
            name:
                "IX_published_occupancy_prices_PriceVersionId_Occupancy",
            schema: "pricing",
            table: "published_occupancy_prices",
            columns: new[] { "PriceVersionId", "Occupancy" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "published_occupancy_prices",
            schema: "pricing");

        migrationBuilder.DropTable(
            name: "price_versions",
            schema: "pricing");
    }
}
