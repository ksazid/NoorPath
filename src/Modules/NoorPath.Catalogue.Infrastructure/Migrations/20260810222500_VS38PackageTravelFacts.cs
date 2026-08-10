using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Catalogue.Infrastructure.Migrations;

[DbContext(typeof(CatalogueDbContext))]
[Migration("20260810222500_VS38PackageTravelFacts")]
public partial class VS38PackageTravelFacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TravelFactsJson",
            schema: "catalogue",
            table: "package_versions",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<int>(
            name: "TravelFactsVersion",
            schema: "catalogue",
            table: "package_versions",
            type: "integer",
            nullable: false,
            defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TravelFactsJson",
            schema: "catalogue",
            table: "package_versions");

        migrationBuilder.DropColumn(
            name: "TravelFactsVersion",
            schema: "catalogue",
            table: "package_versions");
    }
}
