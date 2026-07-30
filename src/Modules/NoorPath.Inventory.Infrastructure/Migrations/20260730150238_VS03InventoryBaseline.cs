using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS03InventoryBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "inventory_configurations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_audits",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_audits_inventory_configurations_InventoryConfigur~",
                        column: x => x.InventoryConfigurationId,
                        principalSchema: "inventory",
                        principalTable: "inventory_configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_pools",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Occupancy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_pools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_pools_inventory_configurations_InventoryConfigura~",
                        column: x => x.InventoryConfigurationId,
                        principalSchema: "inventory",
                        principalTable: "inventory_configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audits_DepartureId_Version",
                schema: "inventory",
                table: "inventory_audits",
                columns: new[] { "DepartureId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audits_InventoryConfigurationId",
                schema: "inventory",
                table: "inventory_audits",
                column: "InventoryConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_configurations_DepartureId",
                schema: "inventory",
                table: "inventory_configurations",
                column: "DepartureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_configurations_OperatorId",
                schema: "inventory",
                table: "inventory_configurations",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_pools_InventoryConfigurationId_Occupancy",
                schema: "inventory",
                table: "inventory_pools",
                columns: new[] { "InventoryConfigurationId", "Occupancy" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_audits",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_pools",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_configurations",
                schema: "inventory");
        }
    }
}
