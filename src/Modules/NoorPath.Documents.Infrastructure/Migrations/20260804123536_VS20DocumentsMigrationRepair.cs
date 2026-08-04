using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Documents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS20DocumentsMigrationRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.CreateTable(
                name: "audit",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "requirements",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DeclaredContentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeclaredSize = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MalwareStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ReviewReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteAfterUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HoldAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_SubmissionId_OccurredAtUtc",
                schema: "documents",
                table: "audit",
                columns: new[] { "SubmissionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_requirements_BookingId_TravellerId_Kind",
                schema: "documents",
                table: "requirements",
                columns: new[] { "BookingId", "TravellerId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_ObjectKey",
                schema: "documents",
                table: "submissions",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_RequirementId_CreatedAtUtc",
                schema: "documents",
                table: "submissions",
                columns: new[] { "RequirementId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "requirements",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "submissions",
                schema: "documents");
        }
    }
}
