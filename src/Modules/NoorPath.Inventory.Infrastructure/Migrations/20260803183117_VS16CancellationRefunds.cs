using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS16CancellationRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_releases",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancellationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReleasedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_releases", x => x.Id);
                    table.CheckConstraint("CK_inventory_releases_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_releases_inventory_commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalSchema: "inventory",
                        principalTable: "inventory_commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_releases_inventory_holds_HoldId",
                        column: x => x.HoldId,
                        principalSchema: "inventory",
                        principalTable: "inventory_holds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_releases_BookingId_ReleasedAtUtc",
                schema: "inventory",
                table: "inventory_releases",
                columns: new[] { "BookingId", "ReleasedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_releases_CancellationRequestId",
                schema: "inventory",
                table: "inventory_releases",
                column: "CancellationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_releases_CommitmentId",
                schema: "inventory",
                table: "inventory_releases",
                column: "CommitmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_releases_HoldId",
                schema: "inventory",
                table: "inventory_releases",
                column: "HoldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_releases",
                schema: "inventory");
        }
    }
}
