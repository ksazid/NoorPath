using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Operators.Infrastructure.Migrations;

public partial class V2OperatorsBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "operators");
        migrationBuilder.CreateTable(
            name: "operators", schema: "operators",
            columns: table => new
            {
                Id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_operators", x => x.Id));
        migrationBuilder.CreateTable(
            name: "operator_memberships", schema: "operators",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OperatorId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                AccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_operator_memberships", x => x.Id);
                table.ForeignKey("FK_operator_memberships_operators_OperatorId", x => x.OperatorId, "operators", "operators", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateTable(
            name: "operator_membership_permissions", schema: "operators",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                Permission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_operator_membership_permissions", x => x.Id);
                table.ForeignKey("FK_operator_membership_permissions_operator_memberships_MembershipId", x => x.MembershipId, "operators", "operator_memberships", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_operator_memberships_AccountId", "operators", "operator_memberships", "AccountId");
        migrationBuilder.CreateIndex("IX_operator_memberships_OperatorId_AccountId", "operators", "operator_memberships", new[] { "OperatorId", "AccountId" }, unique: true);
        migrationBuilder.CreateIndex("IX_operator_membership_permissions_MembershipId_Permission", "operators", "operator_membership_permissions", new[] { "MembershipId", "Permission" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("operator_membership_permissions", "operators");
        migrationBuilder.DropTable("operator_memberships", "operators");
        migrationBuilder.DropTable("operators", "operators");
    }
}
