using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Inventory.Infrastructure.Migrations;

public partial class VS10Commitments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_inventory_holds_State", schema: "inventory", table: "inventory_holds");
        migrationBuilder.AddCheckConstraint(name: "CK_inventory_holds_State", schema: "inventory", table: "inventory_holds", sql: "\"State\" IN ('Active', 'Released', 'Expired', 'Committed')");
        migrationBuilder.CreateTable(
            name: "inventory_commitments", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                PaymentAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                InventoryPoolId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_commitments", x => x.Id);
                table.CheckConstraint("CK_inventory_commitments_Quantity_Positive", "\"Quantity\" > 0");
                table.ForeignKey("FK_inventory_commitments_inventory_holds_HoldId", x => x.HoldId, principalSchema: "inventory", principalTable: "inventory_holds", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_inventory_commitments_inventory_pools_InventoryPoolId", x => x.InventoryPoolId, principalSchema: "inventory", principalTable: "inventory_pools", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_inventory_commitments_BookingId", schema: "inventory", table: "inventory_commitments", column: "BookingId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_inventory_commitments_HoldId", schema: "inventory", table: "inventory_commitments", column: "HoldId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_inventory_commitments_InventoryPoolId_CreatedAtUtc", schema: "inventory", table: "inventory_commitments", columns: new[] { "InventoryPoolId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inventory_commitments", schema: "inventory");
        migrationBuilder.DropCheckConstraint(name: "CK_inventory_holds_State", schema: "inventory", table: "inventory_holds");
        migrationBuilder.AddCheckConstraint(name: "CK_inventory_holds_State", schema: "inventory", table: "inventory_holds", sql: "\"State\" IN ('Active', 'Released', 'Expired')");
    }
}
