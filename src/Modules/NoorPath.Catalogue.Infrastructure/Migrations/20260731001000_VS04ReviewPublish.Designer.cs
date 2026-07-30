using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NoorPath.Catalogue;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoorPath.Catalogue.Infrastructure.Migrations;

[DbContext(typeof(CatalogueDbContext))]
[Migration("20260731001000_VS04ReviewPublish")]
partial class VS04ReviewPublish
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("catalogue")
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.PackageTemplateRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("OperatorId").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("WorkingName").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.HasKey("Id");
            b.HasIndex("OperatorId");
            b.ToTable("package_templates", "catalogue");
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.PackageVersionRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("MadinahClassification").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<FactConfirmationState>("MadinahConfirmationState").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<string>("MadinahDistanceDisclosure").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<string>("MadinahHotelName").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<int>("MadinahNights").HasColumnType("integer");
            b.Property<string>("MakkahClassification").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<FactConfirmationState>("MakkahConfirmationState").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<string>("MakkahDistanceDisclosure").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<string>("MakkahHotelName").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<int>("MakkahNights").HasColumnType("integer");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<Guid>("PackageTemplateId").HasColumnType("uuid");
            b.Property<int>("Sequence").HasColumnType("integer");
            b.Property<CatalogueDraftStatus>("Status").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<string>("Summary").IsRequired().HasMaxLength(600).HasColumnType("character varying(600)");
            b.Property<string>("TravelDetails").IsRequired().HasMaxLength(600).HasColumnType("character varying(600)");
            b.Property<FactConfirmationState>("TravelConfirmationState").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<string>("TravelRouteSummary").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("PackageTemplateId", "Sequence").IsUnique();
            b.ToTable("package_versions", "catalogue");
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.DepartureBatchRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateOnly>("DepartureDate").HasColumnType("date");
            b.Property<string>("OperatorId").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<string>("Origin").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<Guid>("PackageVersionId").HasColumnType("uuid");
            b.Property<DateTimeOffset?>("PublishedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("PublishedByAccountId").HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<int?>("PublishedInventoryVersion").HasColumnType("integer");
            b.Property<Guid?>("PublishedPriceVersionId").HasColumnType("uuid");
            b.Property<int?>("PublishedPricingVersion").HasColumnType("integer");
            b.Property<DateOnly>("ReturnDate").HasColumnType("date");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("SubmittedByAccountId").HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<CatalogueDraftStatus>("Status").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<int>("Version").IsConcurrencyToken().HasColumnType("integer");
            b.HasKey("Id");
            b.HasIndex("OperatorId");
            b.HasIndex("PackageVersionId");
            b.ToTable("departure_batches", "catalogue");
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.PackageContentItemRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<PackageContentKind>("Kind").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<Guid>("PackageVersionId").HasColumnType("uuid");
            b.Property<int>("Position").HasColumnType("integer");
            b.Property<string>("Text").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.HasKey("Id");
            b.HasIndex("PackageVersionId", "Kind", "Position").IsUnique();
            b.ToTable("package_content_items", "catalogue");
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.CatalogueDraftAuditRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<string>("Action").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<string>("ActorAccountId").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<string>("CorrelationId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<Guid>("DepartureBatchId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("Timestamp").HasColumnType("timestamp with time zone");
            b.Property<int>("Version").HasColumnType("integer");
            b.HasKey("Id");
            b.HasIndex("DepartureBatchId", "Version");
            b.ToTable("draft_audits", "catalogue");
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.CatalogueOutboxRecord", b =>
        {
            b.Property<Guid>("EventId").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<Guid>("AggregateId").HasColumnType("uuid");
            b.Property<string>("AggregateType").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
            b.Property<int>("AggregateVersion").HasColumnType("integer");
            b.Property<int>("AttemptCount").HasColumnType("integer");
            b.Property<string>("CorrelationId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("EventType").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<int>("EventVersion").HasColumnType("integer");
            b.Property<DateTimeOffset?>("NextAttemptAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset>("OccurredAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("OperatorId").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<string>("Payload").IsRequired().HasColumnType("jsonb");
            b.Property<DateTimeOffset?>("ProcessedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("ProducerModule").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
            b.Property<string>("State").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.HasKey("EventId");
            b.HasIndex("State", "NextAttemptAtUtc");
            b.ToTable("outbox_messages", "catalogue");
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.PackageVersionRecord", b =>
        {
            b.HasOne("NoorPath.Catalogue.Infrastructure.PackageTemplateRecord", null).WithMany().HasForeignKey("PackageTemplateId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.DepartureBatchRecord", b =>
        {
            b.HasOne("NoorPath.Catalogue.Infrastructure.PackageVersionRecord", null).WithMany().HasForeignKey("PackageVersionId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.PackageContentItemRecord", b =>
        {
            b.HasOne("NoorPath.Catalogue.Infrastructure.PackageVersionRecord", null).WithMany().HasForeignKey("PackageVersionId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("NoorPath.Catalogue.Infrastructure.CatalogueDraftAuditRecord", b =>
        {
            b.HasOne("NoorPath.Catalogue.Infrastructure.DepartureBatchRecord", null).WithMany().HasForeignKey("DepartureBatchId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
