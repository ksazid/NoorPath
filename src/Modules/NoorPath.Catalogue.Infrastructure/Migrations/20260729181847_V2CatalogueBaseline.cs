using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Catalogue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2CatalogueBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalogue");

            migrationBuilder.CreateTable(
                name: "packages",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Tier = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "batches",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureCity = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Route = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Availability = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_batches_packages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalogue",
                        principalTable: "packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inclusions",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inclusions_packages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalogue",
                        principalTable: "packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_versions",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalStartingPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_versions_batches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "catalogue",
                        principalTable: "batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "publication_audits",
                schema: "catalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpectedVersion = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publication_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_publication_audits_batches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "catalogue",
                        principalTable: "batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_batches_PackageId",
                schema: "catalogue",
                table: "batches",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_inclusions_PackageId_Position",
                schema: "catalogue",
                table: "inclusions",
                columns: new[] { "PackageId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_versions_BatchId",
                schema: "catalogue",
                table: "price_versions",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_publication_audits_BatchId",
                schema: "catalogue",
                table: "publication_audits",
                column: "BatchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inclusions",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "price_versions",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "publication_audits",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "batches",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "packages",
                schema: "catalogue");
        }
    }
}
