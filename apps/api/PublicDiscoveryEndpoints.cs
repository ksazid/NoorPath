using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Pricing.Infrastructure;

public static class PublicDiscoveryEndpoints
{
    private const int MaxItems = 50;
    private const int MaxCandidates = 200;

    public static void MapPublicDiscovery(this WebApplication app)
    {
        app.MapGet("/api/v1/departures", ListPublishedDeparturesAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> ListPublishedDeparturesAsync(
        HttpContext http,
        CatalogueDbContext catalogue,
        OperatorsDbContext operators,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        var candidates = await (
            from departure in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on departure.PackageVersionId equals packageVersion.Id
            where
                departure.Status == CatalogueDraftStatus.Published &&
                packageVersion.Status == CatalogueDraftStatus.Published &&
                departure.PublishedPriceVersionId != null
            orderby departure.DepartureDate, departure.Id
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
                packageVersion.TravelConfirmationState))
            .Take(MaxCandidates)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return Results.Ok(new DiscoveryResponse([]));

        var operatorIds = candidates
            .Select(item => item.OperatorId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var eligibleOperators = await operators.Operators.AsNoTracking()
            .Where(item => operatorIds.Contains(item.Id) && item.State == OperatorState.Approved)
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);

        var eligibleCandidates = candidates
            .Where(item => eligibleOperators.ContainsKey(item.OperatorId))
            .ToArray();

        if (eligibleCandidates.Length == 0)
            return Results.Ok(new DiscoveryResponse([]));

        var departureIds = eligibleCandidates.Select(item => item.DepartureId).ToArray();
        var priceVersionIds = eligibleCandidates.Select(item => item.PriceVersionId).ToArray();
        var packageVersionIds = eligibleCandidates.Select(item => item.PackageVersionId).ToArray();

        var priceVersions = await pricing.PriceVersions.AsNoTracking()
            .Where(item => priceVersionIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var priceVersionById = priceVersions.ToDictionary(item => item.Id);

        var publishedPrices = await pricing.PublishedOccupancyPrices.AsNoTracking()
            .Where(item => priceVersionIds.Contains(item.PriceVersionId) && item.Amount > 0)
            .ToListAsync(cancellationToken);
        var pricesByVersion = publishedPrices
            .GroupBy(item => item.PriceVersionId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var inventoryConfigurations = await inventory.Configurations.AsNoTracking()
            .Where(item => departureIds.Contains(item.DepartureId))
            .ToListAsync(cancellationToken);
        var inventoryByDeparture = inventoryConfigurations
            .GroupBy(item => item.DepartureId)
            .ToDictionary(group => group.Key, group => group.Single());
        var inventoryConfigurationIds = inventoryConfigurations.Select(item => item.Id).ToArray();

        var pools = await inventory.Pools.AsNoTracking()
            .Where(item => inventoryConfigurationIds.Contains(item.InventoryConfigurationId) && item.Capacity > 0)
            .ToListAsync(cancellationToken);
        var poolsByConfiguration = pools
            .GroupBy(item => item.InventoryConfigurationId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var inclusionItems = await catalogue.PackageContentItems.AsNoTracking()
            .Where(item => packageVersionIds.Contains(item.PackageVersionId) && item.Kind == PackageContentKind.Inclusion)
            .OrderBy(item => item.PackageVersionId)
            .ThenBy(item => item.Position)
            .ToListAsync(cancellationToken);
        var inclusionsByPackage = inclusionItems
            .GroupBy(item => item.PackageVersionId)
            .ToDictionary(
                group => group.Key,
                group => group.Take(3).Select(item => item.Text).ToArray());

        var items = new List<DiscoveryItem>(Math.Min(MaxItems, eligibleCandidates.Length));

        foreach (var candidate in eligibleCandidates)
        {
            if (items.Count >= MaxItems)
                break;

            if (!priceVersionById.TryGetValue(candidate.PriceVersionId, out var priceVersion))
                continue;

            if (
                priceVersion.DepartureId != candidate.DepartureId ||
                !string.Equals(priceVersion.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
                continue;

            if (!pricesByVersion.TryGetValue(candidate.PriceVersionId, out var candidatePrices))
                continue;

            if (!inventoryByDeparture.TryGetValue(candidate.DepartureId, out var configuration))
                continue;

            if (!string.Equals(configuration.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
                continue;

            if (!poolsByConfiguration.TryGetValue(configuration.Id, out var candidatePools))
                continue;

            var availabilityByOccupancy = candidatePools.ToDictionary(
                item => OccupancyKey(item.Occupancy.ToString()),
                item => item.Capacity,
                StringComparer.Ordinal);

            var saleablePrices = candidatePrices
                .Select(item => new
                {
                    Occupancy = OccupancyKey(item.Occupancy.ToString()),
                    item.Amount
                })
                .Where(item =>
                    item.Amount > 0 &&
                    availabilityByOccupancy.TryGetValue(item.Occupancy, out var available) &&
                    available > 0)
                .OrderBy(item => item.Amount)
                .ThenBy(item => item.Occupancy, StringComparer.Ordinal)
                .ToArray();

            if (saleablePrices.Length == 0)
                continue;

            var headline = saleablePrices[0];
            var occupancyAvailability = saleablePrices
                .Select(item => item.Occupancy)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => OccupancyOrder(item))
                .Select(item => new OccupancyAvailability(item, availabilityByOccupancy[item]))
                .ToArray();

            inclusionsByPackage.TryGetValue(candidate.PackageVersionId, out var inclusionHighlights);

            items.Add(new DiscoveryItem(
                candidate.DepartureId,
                new PublicOperator(candidate.OperatorId, eligibleOperators[candidate.OperatorId]),
                candidate.PackageName,
                candidate.Summary,
                candidate.Origin,
                candidate.DepartureDate,
                candidate.ReturnDate,
                candidate.ReturnDate.DayNumber - candidate.DepartureDate.DayNumber,
                new StaySummary(
                    candidate.MakkahHotelName,
                    candidate.MakkahClassification,
                    candidate.MakkahDistanceDisclosure,
                    candidate.MakkahNights,
                    ConfirmationKey(candidate.MakkahConfirmationState)),
                new StaySummary(
                    candidate.MadinahHotelName,
                    candidate.MadinahClassification,
                    candidate.MadinahDistanceDisclosure,
                    candidate.MadinahNights,
                    ConfirmationKey(candidate.MadinahConfirmationState)),
                ConfirmationKey(candidate.TravelConfirmationState),
                inclusionHighlights ?? [],
                new HeadlinePrice(headline.Amount, priceVersion.Currency, headline.Occupancy),
                new Availability("available", occupancyAvailability)));
        }

        log.LogInformation(
            "Public discovery outcome={Outcome} itemCount={ItemCount} durationMs={DurationMs} correlationId={CorrelationId}",
            "success",
            items.Count,
            (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
            http.TraceIdentifier);

        return Results.Ok(new DiscoveryResponse(items));
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
        FactConfirmationState TravelConfirmationState);

    private sealed record DiscoveryResponse(IReadOnlyList<DiscoveryItem> Items);

    private sealed record DiscoveryItem(
        Guid DepartureId,
        PublicOperator Operator,
        string PackageName,
        string Summary,
        string Origin,
        DateOnly DepartureDate,
        DateOnly ReturnDate,
        int DurationNights,
        StaySummary Makkah,
        StaySummary Madinah,
        string TravelConfirmationState,
        IReadOnlyList<string> InclusionHighlights,
        HeadlinePrice HeadlinePrice,
        Availability Availability);

    private sealed record PublicOperator(string Id, string DisplayName);

    private sealed record StaySummary(
        string HotelName,
        string Classification,
        string DistanceDisclosure,
        int Nights,
        string ConfirmationState);

    private sealed record HeadlinePrice(decimal Amount, string Currency, string Occupancy);

    private sealed record Availability(
        string Status,
        IReadOnlyList<OccupancyAvailability> Occupancies);

    private sealed record OccupancyAvailability(string Occupancy, int AvailableQuantity);
}
