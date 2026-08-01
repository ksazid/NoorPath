using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations;

[Migration("20260801120000_VS10Confirmation")]
public sealed partial class VS10Confirmation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(name: "ConfirmedAtUtc", schema: "booking", table: "bookings", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ConfirmationExceptionCode", schema: "booking", table: "bookings", type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InventoryCommitmentId", schema: "booking", table: "bookings", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "SettledPaymentAttemptId", schema: "booking", table: "bookings", type: "uuid", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConfirmedAtUtc", schema: "booking", table: "bookings");
        migrationBuilder.DropColumn(name: "ConfirmationExceptionCode", schema: "booking", table: "bookings");
        migrationBuilder.DropColumn(name: "InventoryCommitmentId", schema: "booking", table: "bookings");
        migrationBuilder.DropColumn(name: "SettledPaymentAttemptId", schema: "booking", table: "bookings");
    }
}
