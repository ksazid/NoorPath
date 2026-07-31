using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Pricing.Infrastructure;

public static class PublicPackageDetailsEndpoints
{
    public static void MapPublicPackageDetails(this WebApplication app)
    {
        app.MapGet("/api/v1/departures/{departureId:guid}", GetPublishedDepartureAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetPublishedDepartureAsync(
        Guid departureId,
        HttpContext http,
        CatalogueDbContext catalogue,
        OperatorsDbContext operators,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        var candidate = await (
            from departure in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on departure.PackageVersionId equals packageVersion.Id
            where
                departure.Id == departureId &&
                departure.Status == CatalogueDraftStatus.Published &&
                packageVersion.Status == CatalogueDraftStatus.Published &&
                departure.PublishedPriceVersionId != null
            select new CatalogueCandidate(
                departure.Id,
                departure.OperatorId,
                departure.PublishedPriceVersionId!.Value,
                packageVersion.Id,
                packageVersion.Name,
                packageVersion.Summary,
                departure.Origin,
                departure.DepartureDate,
                departure.ReturnDate,
                packageVersion.MakkahHotelName,
                packageVersion.MakkahClassification,
                packageVersion.MakkahDistanceDisclosure,
                packageVersion.MakkahNights,
                packageVersion.MakkahConfirmationState,
                packageVersion.MadinahHotelName,
                packageVersion.MadinahClassification,
                packageVersion.MadinahDistanceDisclosure,
                packageVersion.MadinahNights,
                packageVersion.MadinahConfirmationState,
                packageVersion.TravelRouteSummary,
                packageVersion.TravelDetails,
                packageVersion.TravelConfirmationState))
            .SingleOrDefaultAsync(cancellationToken);

        if (candidate is null)
            return NotFound(log, http, startedAt);

        var operatorRecord = await operators.Operators.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == candidate.OperatorId && item.State == OperatorState.Approved,
                cancellationToken);

        if (operatorRecord is null)
            return NotFound(log, http, startedAt);

        var priceVersion = await pricing.PriceVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == candidate.PriceVersionId, cancellationToken);

        if (
            priceVersion is null ||
            priceVersion.DepartureId != candidate.DepartureId ||
            !string.Equals(priceVersion.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
            return NotFound(log, http, startedAt);

        var publishedPrices = await pricing.PublishedOccupancyPrices.AsNoTracking()
            .Where(item => item.PriceVersionId == priceVersion.Id && item.Amount > 0)
            .ToListAsync(cancellationToken);

        if (publishedPrices.Count == 0)
            return NotFound(log, http, startedAt);

        var inventoryConfiguration = await inventory.Configurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DepartureId == candidate.DepartureId, cancellationToken);

        if (
            inventoryConfiguration is null ||
            !string.Equals(inventoryConfiguration.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
            return NotFound(log, http, startedAt);

        var pools = await inventory.Pools.AsNoTracking()
            .Where(item => item.InventoryConfigurationId == inventoryConfiguration.Id)
            .ToListAsync(cancellationToken);
        var availabilityByOccupancy = pools.ToDictionary(
            item => OccupancyKey(item.Occupancy.ToString()),
            item => item.Capacity,
            StringComparer.Ordinal);

        var occupancies = publishedPrices
            .OrderBy(item => OccupancyOrder(OccupancyKey(item.Occupancy.ToString())))
            .Select(item =>
            {
                var occupancy = OccupancyKey(item.Occupancy.ToString());
                availabilityByOccupancy.TryGetValue(occupancy, out var availableQuantity);
                return new OccupancyDetail(
                    occupancy,
                    item.Amount,
                    availableQuantity,
                    availableQuantity > 0 ? "available" : "unavailable");
            })
            .ToArray();

        if (!occupancies.Any(item => item.Status == "available"))
            return NotFound(log, http, startedAt);

        var contentItems = await catalogue.PackageContentItems.AsNoTracking()
            .Where(item => item.PackageVersionId == candidate.PackageVersionId)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);
        var inclusions = contentItems
            .Where(item => item.Kind == PackageContentKind.Inclusion)
            .Select(item => item.Text)
            .ToArray();
        var exclusions = contentItems
            .Where(item => item.Kind == PackageContentKind.Exclusion)
            .Select(item => item.Text)
            .ToArray();

        var response = new PackageDetailsResponse(
            candidate.DepartureId,
            new PublicOperator(candidate.OperatorId, operatorRecord.DisplayName),
            candidate.PackageName,
            candidate.Summary,
            candidate.Origin,
            candidate.DepartureDate,
            candidate.ReturnDate,
            candidate.ReturnDate.DayNumber - candidate.DepartureDate.DayNumber,
            new StayDetails(
                candidate.MakkahHotelName,
                candidate.MakkahClassification,
                candidate.MakkahDistanceDisclosure,
                candidate.MakkahNights,
                ConfirmationKey(candidate.MakkahConfirmationState)),
            new StayDetails(
                candidate.MadinahHotelName,
                candidate.MadinahClassification,
                candidate.MadinahDistanceDisclosure,
                candidate.MadinahNights,
                ConfirmationKey(candidate.MadinahConfirmationState)),
            new TravelDetails(
                candidate.TravelRouteSummary,
                candidate.TravelDetails,
                ConfirmationKey(candidate.TravelConfirmationState)),
            inclusions,
            exclusions,
            new PublishedPricing(priceVersion.Currency, occupancies));

        log.LogInformation(
            "Public package details outcome={Outcome} durationMs={DurationMs} correlationId={CorrelationId}",
            "success",
            (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
            http.TraceIdentifier);

        return Results.Ok(response);
    }

    private static IResult NotFound(
        ILogger<Program> log,
        HttpContext http,
        DateTimeOffset startedAt)
    {
        log.LogInformation(
            "Public package details outcome={Outcome} durationMs={DurationMs} correlationId={CorrelationId}",
            "not_found",
            (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
            http.TraceIdentifier);

        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Published package not found");
    }

    private static string ConfirmationKey(FactConfirmationState state) => state switch
    {
        FactConfirmationState.Confirmed => "confirmed",
        _ => "pending"
    };

    private static string OccupancyKey(string value) => value.ToLowerInvariant();

    private static int OccupancyOrder(string value) => value switch
    {
        "double" => 0,
        "triple" => 1,
        "quad" => 2,
        _ => 99
    };

    private sealed record CatalogueCandidate(
        Guid DepartureId,
        string OperatorId,
        Guid PriceVersionId,
        Guid PackageVersionId,
        string PackageName,
        string Summary,
        string Origin,
        DateOnly DepartureDate,
        DateOnly ReturnDate,
        string MakkahHotelName,
        string MakkahClassification,
        string MakkahDistanceDisclosure,
        int MakkahNights,
        FactConfirmationState MakkahConfirmationState,
        string MadinahHotelName,
        string MadinahClassification,
        string MadinahDistanceDisclosure,
        int MadinahNights,
        FactConfirmationState MadinahConfirmationState,
        string TravelRouteSummary,
        string TravelDetails,
        FactConfirmationState TravelConfirmationState);

    private sealed record PackageDetailsResponse(
        Guid DepartureId,
        PublicOperator Operator,
        string PackageName,
        string Summary,
        string Origin,
        DateOnly DepartureDate,
        DateOnly ReturnDate,
        int DurationNights,
        StayDetails Makkah,
        StayDetails Madinah,
        TravelDetails Travel,
        IReadOnlyList<string> Inclusions,
        IReadOnlyList<string> Exclusions,
        PublishedPricing Pricing);

    private sealed record PublicOperator(string Id, string DisplayName);

    private sealed record StayDetails(
        string HotelName,
        string Classification,
        string DistanceDisclosure,
        int Nights,
        string ConfirmationState);

    private sealed record TravelDetails(
        string RouteSummary,
        string Details,
        string ConfirmationState);

    private sealed record PublishedPricing(
        string Currency,
        IReadOnlyList<OccupancyDetail> Occupancies);

    private sealed record OccupancyDetail(
        string Occupancy,
        decimal Amount,
        int AvailableQuantity,
        string Status);
}
