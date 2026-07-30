using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorPath.Catalogue.Infrastructure.Migrations;

public partial class VS04ReviewPublish : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "PublishedAtUtc",
            schema: "catalogue",
            table: "departure_batches",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PublishedByAccountId",
            schema: "catalogue",
            table: "departure_batches",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PublishedInventoryVersion",
            schema: "catalogue",
            table: "departure_batches",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PublishedPriceVersionId",
            schema: "catalogue",
            table: "departure_batches",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PublishedPricingVersion",
            schema: "catalogue",
            table: "departure_batches",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "SubmittedAtUtc",
            schema: "catalogue",
            table: "departure_batches",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SubmittedByAccountId",
            schema: "catalogue",
            table: "departure_batches",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "catalogue",
            columns: table => new
            {
                EventId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                EventType = table.Column<string>(
                    type: "character varying(80)",
                    maxLength: 80,
                    nullable: false),
                EventVersion = table.Column<int>(
                    type: "integer",
                    nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ProducerModule = table.Column<string>(
                    type: "character varying(40)",
                    maxLength: 40,
                    nullable: false),
                AggregateType = table.Column<string>(
                    type: "character varying(40)",
                    maxLength: 40,
                    nullable: false),
                AggregateId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                AggregateVersion = table.Column<int>(
                    type: "integer",
                    nullable: false),
                CorrelationId = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                OperatorId = table.Column<string>(
                    type: "character varying(80)",
                    maxLength: 80,
                    nullable: false),
                Payload = table.Column<string>(
                    type: "jsonb",
                    nullable: false),
                State = table.Column<string>(
                    type: "character varying(20)",
                    maxLength: 20,
                    nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                AttemptCount = table.Column<int>(
                    type: "integer",
                    nullable: false),
                NextAttemptAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                ProcessedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", item => item.EventId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_State_NextAttemptAtUtc",
            schema: "catalogue",
            table: "outbox_messages",
            columns: new[] { "State", "NextAttemptAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "catalogue");

        migrationBuilder.DropColumn(
            name: "PublishedAtUtc",
            schema: "catalogue",
            table: "departure_batches");

        migrationBuilder.DropColumn(
            name: "PublishedByAccountId",
            schema: "catalogue",
            table: "departure_batches");

        migrationBuilder.DropColumn(
            name: "PublishedInventoryVersion",
            schema: "catalogue",
            table: "departure_batches");

        migrationBuilder.DropColumn(
            name: "PublishedPriceVersionId",
            schema: "catalogue",
            table: "departure_batches");

        migrationBuilder.DropColumn(
            name: "PublishedPricingVersion",
            schema: "catalogue",
            table: "departure_batches");

        migrationBuilder.DropColumn(
            name: "SubmittedAtUtc",
            schema: "catalogue",
            table: "departure_batches");

        migrationBuilder.DropColumn(
            name: "SubmittedByAccountId",
            schema: "catalogue",
            table: "departure_batches");
    }
}
