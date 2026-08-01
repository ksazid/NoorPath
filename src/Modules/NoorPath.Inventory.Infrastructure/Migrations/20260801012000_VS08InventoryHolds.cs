using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS08InventoryHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_holds",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryPoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Occupancy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IdempotencyKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TerminalAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_holds", x => x.Id);
                    table.CheckConstraint("CK_inventory_holds_Expiry_After_Creation", "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_inventory_holds_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_inventory_holds_State", "\"State\" IN ('Active', 'Released', 'Expired')");
                    table.ForeignKey(
                        name: "FK_inventory_holds_inventory_pools_InventoryPoolId",
                        column: x => x.InventoryPoolId,
                        principalSchema: "inventory",
                        principalTable: "inventory_pools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_holds_AccountId_DepartureId_Occupancy",
                schema: "inventory",
                table: "inventory_holds",
                columns: new[] { "AccountId", "DepartureId", "Occupancy" },
                unique: true,
                filter: "\"State\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_holds_AccountId_DepartureId_Occupancy_State",
                schema: "inventory",
                table: "inventory_holds",
                columns: new[] { "AccountId", "DepartureId", "Occupancy", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_holds_AccountId_IdempotencyKeyHash",
                schema: "inventory",
                table: "inventory_holds",
                columns: new[] { "AccountId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_holds_InventoryPoolId_State_ExpiresAtUtc",
                schema: "inventory",
                table: "inventory_holds",
                columns: new[] { "InventoryPoolId", "State", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_holds_QuoteId",
                schema: "inventory",
                table: "inventory_holds",
                column: "QuoteId",
                unique: true,
                filter: "\"State\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_holds_QuoteId_State",
                schema: "inventory",
                table: "inventory_holds",
                columns: new[] { "QuoteId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_holds",
                schema: "inventory");
        }
    }
}
