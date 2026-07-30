using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;

public static class PublicationEndpoints
{
    private const string PlatformApproverConfiguration =
        "Authorization:PlatformPublicationApproverAccountIds";

    public static void MapPublicationReview(this WebApplication app)
    {
        var operatorGroup = app.MapGroup("/api/v1/operator/departures")
            .RequireAuthorization();
        operatorGroup.MapGet(
            "/{departureId:guid}/publication-review",
            GetOperatorReviewAsync);
        operatorGroup.MapPost(
            "/{departureId:guid}/submit-review",
            SubmitReviewAsync);

        var platformGroup = app.MapGroup("/api/v1/platform/publications")
            .RequireAuthorization();
        platformGroup.MapGet("", ListPendingAsync);
        platformGroup.MapGet("/{departureId:guid}", GetPlatformReviewAsync);
        platformGroup.MapPost("/{departureId:guid}/publish", PublishAsync);
    }

    private static async Task<IResult> GetOperatorReviewAsync(
        Guid departureId,
        HttpContext http,
        IOperatorAccess operators,
        IOperatorPublicationEligibility publicationEligibility,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(
            http,
            operators,
            cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        var review = await BuildReviewAsync(
            departureId,
            authorization.Access!.OperatorId,
            publicationEligibility,
            catalogue,
            pricing,
            inventory,
            cancellationToken);

        return review is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(review));
    }

    private static async Task<IResult> SubmitReviewAsync(
        Guid departureId,
        PublicationVersionRequest request,
        HttpContext http,
        IOperatorAccess operators,
        IOperatorPublicationEligibility publicationEligibility,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(
            http,
            operators,
            cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        var departure = await catalogue.DepartureBatches.SingleOrDefaultAsync(
            item =>
                item.Id == departureId &&
                item.OperatorId == authorization.Access!.OperatorId,
            cancellationToken);
        if (departure is null)
            return Results.NotFound();

        if (departure.Status != CatalogueDraftStatus.Draft)
            return InvalidTransition(
                http,
                "departure_not_reviewable",
                "Only an editable draft can be submitted for review.");

        var review = await BuildReviewAsync(
            departureId,
            authorization.Access.OperatorId,
            publicationEligibility,
            catalogue,
            pricing,
            inventory,
            cancellationToken);
        if (review is null)
            return Results.NotFound();

        if (!VersionsMatch(review, request))
            return StaleReview(http);

        if (!review.Ready)
            return NotReady(http, review.Checks);

        var packageVersion = await catalogue.PackageVersions.SingleAsync(
            item => item.Id == departure.PackageVersionId,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;

        departure.Status = CatalogueDraftStatus.ReadyForReview;
        departure.Version++;
        departure.SubmittedAtUtc = now;
        departure.SubmittedByAccountId =
            authorization.Principal!.AccountId.Value;
        departure.UpdatedAtUtc = now;
        packageVersion.Status = CatalogueDraftStatus.ReadyForReview;
        packageVersion.UpdatedAtUtc = now;
        catalogue.DraftAudits.Add(CreateAudit(
            departure.Id,
            authorization.Principal.AccountId.Value,
            http.TraceIdentifier,
            "submitted_for_review",
            departure.Version,
            now));

        try
        {
            await catalogue.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StaleReview(http);
        }

        log.LogInformation(
            "Publication review outcome={Outcome} actorAccountId={ActorAccountId} operatorId={OperatorId} departureId={DepartureId} departureVersion={DepartureVersion} pricingVersion={PricingVersion} inventoryVersion={InventoryVersion} correlationId={CorrelationId}",
            "submitted",
            authorization.Principal.AccountId,
            authorization.Access.OperatorId,
            departure.Id,
            departure.Version,
            review.PricingVersion,
            review.InventoryVersion,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            departureId = departure.Id,
            status = StatusKey(departure.Status),
            departureVersion = departure.Version,
            pricingVersion = review.PricingVersion,
            inventoryVersion = review.InventoryVersion,
            submittedAtUtc = now
        });
    }

    private static async Task<IResult> ListPendingAsync(
        HttpContext http,
        IConfiguration configuration,
        CatalogueDbContext catalogue,
        CancellationToken cancellationToken)
    {
        var authorization = ResolvePlatformApprover(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        var pending = await (
            from departure in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on departure.PackageVersionId equals packageVersion.Id
            where departure.Status == CatalogueDraftStatus.ReadyForReview
            orderby departure.SubmittedAtUtc, departure.Id
            select new
            {
                departureId = departure.Id,
                operatorId = departure.OperatorId,
                packageName = packageVersion.Name,
                origin = departure.Origin,
                departureDate = departure.DepartureDate,
                returnDate = departure.ReturnDate,
                departureVersion = departure.Version,
                submittedAtUtc = departure.SubmittedAtUtc
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items = pending });
    }

    private static async Task<IResult> GetPlatformReviewAsync(
        Guid departureId,
        HttpContext http,
        IConfiguration configuration,
        IOperatorPublicationEligibility publicationEligibility,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        CancellationToken cancellationToken)
    {
        var authorization = ResolvePlatformApprover(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        var review = await BuildReviewAsync(
            departureId,
            null,
            publicationEligibility,
            catalogue,
            pricing,
            inventory,
            cancellationToken);

        return review is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(review));
    }

    private static async Task<IResult> PublishAsync(
        Guid departureId,
        PublicationVersionRequest request,
        HttpContext http,
        IConfiguration configuration,
        IOperatorPublicationEligibility publicationEligibility,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = ResolvePlatformApprover(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        var departure = await catalogue.DepartureBatches.SingleOrDefaultAsync(
            item => item.Id == departureId,
            cancellationToken);
        if (departure is null)
            return Results.NotFound();

        if (departure.Status != CatalogueDraftStatus.ReadyForReview)
            return InvalidTransition(
                http,
                "departure_not_reviewable",
                "Only a submitted departure can be approved and published.");

        if (
            string.Equals(
                departure.SubmittedByAccountId,
                authorization.Principal!.AccountId.Value,
                StringComparison.Ordinal))
            return Results.Problem(
                statusCode: 403,
                title: "Independent approval required",
                detail:
                    "The account that submitted this departure cannot approve its publication.",
                extensions: ProblemExtensions(
                    http,
                    "dual_control_required"));

        var review = await BuildReviewAsync(
            departureId,
            null,
            publicationEligibility,
            catalogue,
            pricing,
            inventory,
            cancellationToken);
        if (review is null)
            return Results.NotFound();

        if (!VersionsMatch(review, request))
            return StaleReview(http);

        if (!review.Ready)
            return NotReady(http, review.Checks);

        var pricePlan = await pricing.PricePlans.SingleAsync(
            item =>
                item.DepartureId == departureId &&
                item.OperatorId == departure.OperatorId,
            cancellationToken);
        var priceVersion = await pricing.PriceVersions.SingleOrDefaultAsync(
            item =>
                item.PricePlanId == pricePlan.Id &&
                item.SourcePlanVersion == pricePlan.Version,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (priceVersion is null)
        {
            var prices = await pricing.OccupancyPrices.AsNoTracking()
                .Where(item => item.PricePlanId == pricePlan.Id)
                .OrderBy(item => item.Occupancy)
                .ToListAsync(cancellationToken);
            priceVersion = new PriceVersionRecord
            {
                Id = Guid.NewGuid(),
                PricePlanId = pricePlan.Id,
                DepartureId = departure.Id,
                OperatorId = departure.OperatorId,
                SourcePlanVersion = pricePlan.Version,
                Currency = pricePlan.Currency,
                PublishedByAccountId =
                    authorization.Principal!.AccountId.Value,
                PublishedAtUtc = now
            };
            pricing.PriceVersions.Add(priceVersion);
            pricing.PublishedOccupancyPrices.AddRange(prices.Select(item =>
                new PublishedOccupancyPriceRecord
                {
                    Id = Guid.NewGuid(),
                    PriceVersionId = priceVersion.Id,
                    Occupancy = item.Occupancy,
                    Amount = item.Amount
                }));
            await pricing.SaveChangesAsync(cancellationToken);
        }

        var packageVersion = await catalogue.PackageVersions.SingleAsync(
            item => item.Id == departure.PackageVersionId,
            cancellationToken);
        departure.Status = CatalogueDraftStatus.Published;
        departure.Version++;
        departure.PublishedAtUtc = now;
        departure.PublishedByAccountId =
            authorization.Principal!.AccountId.Value;
        departure.PublishedPriceVersionId = priceVersion.Id;
        departure.PublishedPricingVersion = review.PricingVersion;
        departure.PublishedInventoryVersion = review.InventoryVersion;
        departure.UpdatedAtUtc = now;
        packageVersion.Status = CatalogueDraftStatus.Published;
        packageVersion.UpdatedAtUtc = now;

        catalogue.DraftAudits.Add(CreateAudit(
            departure.Id,
            authorization.Principal.AccountId.Value,
            http.TraceIdentifier,
            "published",
            departure.Version,
            now));
        catalogue.OutboxMessages.AddRange(
            CreateOutbox(
                "PackageVersionPublished",
                packageVersion.Id,
                departure,
                priceVersion.Id,
                http.TraceIdentifier,
                now),
            CreateOutbox(
                "DeparturePublished",
                departure.Id,
                departure,
                priceVersion.Id,
                http.TraceIdentifier,
                now));

        try
        {
            await catalogue.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StaleReview(http);
        }

        log.LogInformation(
            "Publication review outcome={Outcome} actorAccountId={ActorAccountId} operatorId={OperatorId} departureId={DepartureId} departureVersion={DepartureVersion} pricingVersion={PricingVersion} inventoryVersion={InventoryVersion} correlationId={CorrelationId}",
            "published",
            authorization.Principal.AccountId,
            departure.OperatorId,
            departure.Id,
            departure.Version,
            review.PricingVersion,
            review.InventoryVersion,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            departureId = departure.Id,
            packageVersionId = packageVersion.Id,
            priceVersionId = priceVersion.Id,
            status = StatusKey(departure.Status),
            departureVersion = departure.Version,
            publishedAtUtc = now
        });
    }

    private static async Task<PublicationReview?> BuildReviewAsync(
        Guid departureId,
        string? operatorId,
        IOperatorPublicationEligibility publicationEligibility,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        CancellationToken cancellationToken)
    {
        var departure = await catalogue.DepartureBatches.AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == departureId &&
                    (operatorId == null || item.OperatorId == operatorId),
                cancellationToken);
        if (departure is null)
            return null;

        var packageVersion = await catalogue.PackageVersions.AsNoTracking()
            .SingleAsync(
                item => item.Id == departure.PackageVersionId,
                cancellationToken);
        var items = await catalogue.PackageContentItems.AsNoTracking()
            .Where(item => item.PackageVersionId == packageVersion.Id)
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.Position)
            .ToListAsync(cancellationToken);
        var pricePlan = await pricing.PricePlans.AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.DepartureId == departure.Id &&
                    item.OperatorId == departure.OperatorId,
                cancellationToken);
        IReadOnlyList<OccupancyPriceRecord> prices = pricePlan is null
            ? Array.Empty<OccupancyPriceRecord>()
            : await pricing.OccupancyPrices.AsNoTracking()
                .Where(item => item.PricePlanId == pricePlan.Id)
                .OrderBy(item => item.Occupancy)
                .ToListAsync(cancellationToken);
        var inventoryConfiguration = await inventory.Configurations.AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.DepartureId == departure.Id &&
                    item.OperatorId == departure.OperatorId,
                cancellationToken);
        IReadOnlyList<InventoryPoolRecord> pools =
            inventoryConfiguration is null
            ? Array.Empty<InventoryPoolRecord>()
            : await inventory.Pools.AsNoTracking()
                .Where(
                    item =>
                        item.InventoryConfigurationId ==
                        inventoryConfiguration.Id)
                .OrderBy(item => item.Occupancy)
                .ToListAsync(cancellationToken);
        var eligibility = await publicationEligibility
            .FindPublicationEligibilityAsync(
                departure.OperatorId,
                cancellationToken);

        var draftErrors = PackageDepartureDraft.Validate(
            new(
                packageVersion.Name,
                packageVersion.Summary,
                new(
                    packageVersion.MakkahHotelName,
                    packageVersion.MakkahClassification,
                    packageVersion.MakkahDistanceDisclosure,
                    packageVersion.MakkahNights,
                    packageVersion.MakkahConfirmationState),
                new(
                    packageVersion.MadinahHotelName,
                    packageVersion.MadinahClassification,
                    packageVersion.MadinahDistanceDisclosure,
                    packageVersion.MadinahNights,
                    packageVersion.MadinahConfirmationState),
                new(
                    packageVersion.TravelRouteSummary,
                    packageVersion.TravelDetails,
                    packageVersion.TravelConfirmationState),
                departure.Origin,
                departure.DepartureDate,
                departure.ReturnDate,
                items
                    .Where(item =>
                        item.Kind == PackageContentKind.Inclusion)
                    .Select(item => item.Text)
                    .ToArray(),
                items
                    .Where(item =>
                        item.Kind == PackageContentKind.Exclusion)
                    .Select(item => item.Text)
                    .ToArray()));

        var priceKeys = prices
            .Select(item => item.Occupancy.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var poolKeys = pools
            .Select(item => item.Occupancy.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var commercialKeysMatch =
            priceKeys.Count > 0 &&
            priceKeys.SetEquals(poolKeys);
        var everyPriceIsPositive =
            prices.Count > 0 &&
            prices.All(item => item.Amount > 0);
        var everyCapacityIsPositive =
            pools.Count > 0 &&
            pools.All(item => item.Capacity > 0);

        var checks = new[]
        {
            new PublicationCheck(
                "operator",
                "Operator eligible",
                eligibility?.CanPublish == true,
                eligibility?.CanPublish == true
                    ? "The owning operator is approved."
                    : "The owning operator is not currently eligible to publish."),
            new PublicationCheck(
                "catalogue",
                "Catalogue complete",
                draftErrors.Count == 0,
                draftErrors.Count == 0
                    ? "Required package and departure facts are valid."
                    : "Required catalogue facts need correction."),
            new PublicationCheck(
                "pricing",
                "Pricing valid",
                pricePlan is not null && everyPriceIsPositive,
                pricePlan is not null && everyPriceIsPositive
                    ? $"{prices.Count} occupancy price(s) use {pricePlan.Currency}."
                    : "Configure at least one valid occupancy price."),
            new PublicationCheck(
                "inventory",
                "Inventory valid",
                inventoryConfiguration is not null && everyCapacityIsPositive,
                inventoryConfiguration is not null && everyCapacityIsPositive
                    ? $"{pools.Count} occupancy pool(s) have positive capacity."
                    : "Every offered occupancy needs positive capacity."),
            new PublicationCheck(
                "occupancies",
                "Commercial rows match",
                commercialKeysMatch,
                commercialKeysMatch
                    ? "Pricing and Inventory use the same occupancy keys."
                    : "Pricing and Inventory occupancy keys must match.")
        };

        return new(
            departure,
            packageVersion,
            items,
            pricePlan,
            prices,
            inventoryConfiguration,
            pools,
            checks);
    }

    private static object ToResponse(PublicationReview review) => new
    {
        departureId = review.Departure.Id,
        operatorId = review.Departure.OperatorId,
        status = StatusKey(review.Departure.Status),
        departureVersion = review.Departure.Version,
        pricingVersion = review.PricingVersion,
        inventoryVersion = review.InventoryVersion,
        ready = review.Ready,
        checks = review.Checks.Select(item => new
        {
            key = item.Key,
            label = item.Label,
            passed = item.Passed,
            detail = item.Detail
        }),
        package = new
        {
            packageVersionId = review.PackageVersion.Id,
            name = review.PackageVersion.Name,
            summary = review.PackageVersion.Summary,
            makkah = new
            {
                hotelName = review.PackageVersion.MakkahHotelName,
                classification = review.PackageVersion.MakkahClassification,
                distanceDisclosure =
                    review.PackageVersion.MakkahDistanceDisclosure,
                nights = review.PackageVersion.MakkahNights,
                confirmationState = review.PackageVersion
                    .MakkahConfirmationState
                    .ToString()
                    .ToLowerInvariant()
            },
            madinah = new
            {
                hotelName = review.PackageVersion.MadinahHotelName,
                classification = review.PackageVersion.MadinahClassification,
                distanceDisclosure =
                    review.PackageVersion.MadinahDistanceDisclosure,
                nights = review.PackageVersion.MadinahNights,
                confirmationState = review.PackageVersion
                    .MadinahConfirmationState
                    .ToString()
                    .ToLowerInvariant()
            },
            travel = new
            {
                routeSummary = review.PackageVersion.TravelRouteSummary,
                details = review.PackageVersion.TravelDetails,
                confirmationState = review.PackageVersion
                    .TravelConfirmationState
                    .ToString()
                    .ToLowerInvariant()
            },
            origin = review.Departure.Origin,
            departureDate = review.Departure.DepartureDate,
            returnDate = review.Departure.ReturnDate,
            inclusions = review.Items
                .Where(item => item.Kind == PackageContentKind.Inclusion)
                .Select(item => item.Text),
            exclusions = review.Items
                .Where(item => item.Kind == PackageContentKind.Exclusion)
                .Select(item => item.Text)
        },
        pricing = review.PricePlan is null
            ? null
            : new
            {
                currency = review.PricePlan.Currency,
                version = review.PricePlan.Version,
                occupancies = review.Prices.Select(item => new
                {
                    occupancy = item.Occupancy
                        .ToString()
                        .ToLowerInvariant(),
                    amount = item.Amount
                })
            },
        inventory = review.InventoryConfiguration is null
            ? null
            : new
            {
                version = review.InventoryConfiguration.Version,
                pools = review.Pools.Select(item => new
                {
                    occupancy = item.Occupancy
                        .ToString()
                        .ToLowerInvariant(),
                    capacity = item.Capacity,
                    availableQuantity = item.Capacity
                })
            }
    };

    private static bool VersionsMatch(
        PublicationReview review,
        PublicationVersionRequest request) =>
        review.Departure.Version == request.ExpectedDepartureVersion &&
        review.PricingVersion == request.ExpectedPricingVersion &&
        review.InventoryVersion == request.ExpectedInventoryVersion;

    private static CatalogueDraftAuditRecord CreateAudit(
        Guid departureId,
        string actorAccountId,
        string correlationId,
        string action,
        int version,
        DateTimeOffset timestamp) => new()
        {
            Id = Guid.NewGuid(),
            DepartureBatchId = departureId,
            ActorAccountId = actorAccountId,
            CorrelationId = correlationId,
            Action = action,
            Version = version,
            Timestamp = timestamp
        };

    private static CatalogueOutboxRecord CreateOutbox(
        string eventType,
        Guid aggregateId,
        DepartureBatchRecord departure,
        Guid priceVersionId,
        string correlationId,
        DateTimeOffset now)
    {
        var payload = JsonSerializer.Serialize(new
        {
            departureId = departure.Id,
            operatorId = departure.OperatorId,
            packageVersionId = departure.PackageVersionId,
            priceVersionId,
            departureVersion = departure.Version,
            pricingVersion = departure.PublishedPricingVersion,
            inventoryVersion = departure.PublishedInventoryVersion
        });

        return new()
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventVersion = 1,
            OccurredAtUtc = now,
            ProducerModule = "Catalogue",
            AggregateType = eventType.StartsWith(
                "PackageVersion",
                StringComparison.Ordinal)
                ? "PackageVersion"
                : "Departure",
            AggregateId = aggregateId,
            AggregateVersion = departure.Version,
            CorrelationId = correlationId,
            OperatorId = departure.OperatorId,
            Payload = payload,
            State = "Pending",
            CreatedAtUtc = now,
            AttemptCount = 0
        };
    }

    private static async Task<OperatorAuthorization> ResolveOperatorAsync(
        HttpContext http,
        IOperatorAccess operators,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return new(
                null,
                null,
                Results.Problem(
                    statusCode: 401,
                    title: "Sign in required",
                    extensions: ProblemExtensions(
                        http,
                        "not_authenticated")));

        var access = await operators.FindActiveMembershipAsync(
            principal.AccountId,
            cancellationToken);
        if (
            access is null ||
            !access.IsAllowed(OperatorPermissions.AdminAccess))
            return new(
                principal,
                access,
                Results.Problem(
                    statusCode: 403,
                    title: "Access unavailable",
                    detail:
                        "This account does not have access to operator administration.",
                    extensions: ProblemExtensions(http, "forbidden")));

        return new(principal, access, null);
    }

    private static PlatformAuthorization ResolvePlatformApprover(
        HttpContext http,
        IConfiguration configuration)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return new(
                null,
                Results.Problem(
                    statusCode: 401,
                    title: "Sign in required",
                    extensions: ProblemExtensions(
                        http,
                        "not_authenticated")));

        var isAllowed = configuration
            .GetSection(PlatformApproverConfiguration)
            .GetChildren()
            .Select(item => item.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Contains(
                principal.AccountId.Value,
                StringComparer.Ordinal);
        if (!isAllowed)
            return new(
                principal,
                Results.Problem(
                    statusCode: 403,
                    title: "Publication access unavailable",
                    detail:
                        "This account is not authorized to approve NoorPath publications.",
                    extensions: ProblemExtensions(http, "forbidden")));

        return new(principal, null);
    }

    private static IResult StaleReview(HttpContext http) =>
        Results.Problem(
            statusCode: 409,
            title: "Publication review changed",
            detail:
                "Catalogue, pricing or inventory changed. Reload the review before continuing.",
            extensions: ProblemExtensions(
                http,
                "stale_publication_review"));

    private static IResult InvalidTransition(
        HttpContext http,
        string code,
        string detail) =>
        Results.Problem(
            statusCode: 409,
            title: "Publication transition unavailable",
            detail: detail,
            extensions: ProblemExtensions(http, code));

    private static IResult NotReady(
        HttpContext http,
        IReadOnlyList<PublicationCheck> checks)
    {
        var extensions = ProblemExtensions(
            http,
            "publication_not_ready");
        extensions["checks"] = checks
            .Where(item => !item.Passed)
            .Select(item => new
            {
                key = item.Key,
                label = item.Label,
                detail = item.Detail
            })
            .ToArray();
        return Results.Problem(
            statusCode: 422,
            title: "Departure is not ready to publish",
            detail:
                "Resolve every failed review check before submitting or publishing.",
            extensions: extensions);
    }

    private static Dictionary<string, object?> ProblemExtensions(
        HttpContext http,
        string code) => new()
        {
            ["code"] = code,
            ["correlationId"] = http.TraceIdentifier
        };

    private static string StatusKey(CatalogueDraftStatus status) => status switch
    {
        CatalogueDraftStatus.ReadyForReview => "readyForReview",
        CatalogueDraftStatus.Published => "published",
        _ => "draft"
    };

    private sealed record PublicationReview(
        DepartureBatchRecord Departure,
        PackageVersionRecord PackageVersion,
        IReadOnlyList<PackageContentItemRecord> Items,
        PricePlanRecord? PricePlan,
        IReadOnlyList<OccupancyPriceRecord> Prices,
        InventoryConfigurationRecord? InventoryConfiguration,
        IReadOnlyList<InventoryPoolRecord> Pools,
        IReadOnlyList<PublicationCheck> Checks)
    {
        public int PricingVersion => PricePlan?.Version ?? 0;
        public int InventoryVersion =>
            InventoryConfiguration?.Version ?? 0;
        public bool Ready => Checks.All(item => item.Passed);
    }

    private sealed record PublicationCheck(
        string Key,
        string Label,
        bool Passed,
        string Detail);

    private sealed record OperatorAuthorization(
        CurrentPrincipal? Principal,
        OperatorAccess? Access,
        IResult? Error);

    private sealed record PlatformAuthorization(
        CurrentPrincipal? Principal,
        IResult? Error);
}

public sealed record PublicationVersionRequest(
    int ExpectedDepartureVersion,
    int ExpectedPricingVersion,
    int ExpectedInventoryVersion);
