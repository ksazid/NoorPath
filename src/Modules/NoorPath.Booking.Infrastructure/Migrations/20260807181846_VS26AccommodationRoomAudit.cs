using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VS26AccommodationRoomAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TravellerId",
                schema: "booking",
                table: "accommodation_assignment_audits",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TravellerId",
                schema: "booking",
                table: "accommodation_assignment_audits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
