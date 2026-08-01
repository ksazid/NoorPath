using Microsoft.EntityFrameworkCore.Migrations;
namespace NoorPath.Documents.Infrastructure.Migrations;

[Migration("20260802120000_VS12Documents")]
public sealed class VS12Documents : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.EnsureSchema("documents");
        m.Sql("""
CREATE TABLE documents.requirements ("Id" uuid PRIMARY KEY, "BookingId" uuid NOT NULL, "TravellerId" uuid NOT NULL, "PolicyVersion" varchar(16) NOT NULL, "Kind" varchar(32) NOT NULL, "CreatedAtUtc" timestamptz NOT NULL);
CREATE UNIQUE INDEX "IX_requirements_BookingId_TravellerId_Kind" ON documents.requirements ("BookingId","TravellerId","Kind");
CREATE TABLE documents.submissions ("Id" uuid PRIMARY KEY, "RequirementId" uuid NOT NULL, "ObjectKey" varchar(160) NOT NULL, "DeclaredContentType" varchar(40) NOT NULL, "DeclaredSize" bigint NOT NULL, "State" varchar(32) NOT NULL, "MalwareStatus" varchar(24) NOT NULL, "ReviewReason" varchar(500), "ReviewedBy" text, "ReviewedAtUtc" timestamptz, "CreatedAtUtc" timestamptz NOT NULL, "DeleteAfterUtc" timestamptz, "HoldAtUtc" timestamptz, "Version" integer NOT NULL);
CREATE UNIQUE INDEX "IX_submissions_ObjectKey" ON documents.submissions ("ObjectKey"); CREATE INDEX "IX_submissions_RequirementId_CreatedAtUtc" ON documents.submissions ("RequirementId","CreatedAtUtc");
CREATE TABLE documents.audit ("Id" uuid PRIMARY KEY, "SubmissionId" uuid NOT NULL, "Action" varchar(48) NOT NULL, "ActorId" varchar(120) NOT NULL, "Purpose" varchar(500) NOT NULL, "OccurredAtUtc" timestamptz NOT NULL);
CREATE INDEX "IX_audit_SubmissionId_OccurredAtUtc" ON documents.audit ("SubmissionId","OccurredAtUtc");
""");
    }
    protected override void Down(MigrationBuilder m) { m.DropTable("audit", "documents"); m.DropTable("submissions", "documents"); m.DropTable("requirements", "documents"); }
}
