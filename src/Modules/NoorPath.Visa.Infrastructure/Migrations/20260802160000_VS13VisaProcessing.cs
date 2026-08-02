using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace NoorPath.Visa.Infrastructure.Migrations;

public partial class VS13VisaProcessing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "visa");
        migrationBuilder.CreateTable(name: "cases", schema: "visa", columns: table => new { Id = table.Column<Guid>(type: "uuid", nullable: false), BookingId = table.Column<Guid>(type: "uuid", nullable: false), TravellerId = table.Column<Guid>(type: "uuid", nullable: false), OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false), Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), CustomerAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true), Version = table.Column<int>(type: "integer", nullable: false), CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) }, constraints: table => table.PrimaryKey("PK_cases", x => x.Id));
        migrationBuilder.CreateTable(name: "transitions", schema: "visa", columns: table => new { Id = table.Column<Guid>(type: "uuid", nullable: false), CaseId = table.Column<Guid>(type: "uuid", nullable: false), PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), ActorId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false), Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true), Version = table.Column<int>(type: "integer", nullable: false), OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) }, constraints: table => table.PrimaryKey("PK_transitions", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_cases_BookingId_TravellerId", schema: "visa", table: "cases", columns: new[] { "BookingId", "TravellerId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_cases_OperatorId_Status_UpdatedAtUtc", schema: "visa", table: "cases", columns: new[] { "OperatorId", "Status", "UpdatedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_transitions_CaseId_OccurredAtUtc", schema: "visa", table: "transitions", columns: new[] { "CaseId", "OccurredAtUtc" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable(name: "transitions", schema: "visa"); migrationBuilder.DropTable(name: "cases", schema: "visa"); }
}
