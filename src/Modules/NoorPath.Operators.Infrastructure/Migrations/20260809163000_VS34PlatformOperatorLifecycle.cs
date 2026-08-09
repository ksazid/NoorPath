using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Operators.Infrastructure.Migrations;

public partial class VS34PlatformOperatorLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "operator_state_audits",
            schema: "operators",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                FromState = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                ToState = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                ActorAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                OperatorVersion = table.Column<int>(type: "integer", nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_operator_state_audits", x => x.Id);
                table.ForeignKey(
                    name: "FK_operator_state_audits_operators_OperatorId",
                    column: x => x.OperatorId,
                    principalSchema: "operators",
                    principalTable: "operators",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_operator_state_audits_OperatorId_Timestamp",
            schema: "operators",
            table: "operator_state_audits",
            columns: new[] { "OperatorId", "Timestamp" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "operator_state_audits",
            schema: "operators");
    }
}
