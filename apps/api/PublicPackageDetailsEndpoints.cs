using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Pricing;
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
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
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
            return NotFound(log, http, startedAt, timeProvider);

        var operatorRecord = await operators.Operators.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == candidate.OperatorId && item.State == OperatorState.Approved,
                cancellationToken);
        if (operatorRecord is null)
            return NotFound(log, http, startedAt, timeProvider);

        var priceVersion = await pricing.PriceVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == candidate.PriceVersionId, cancellationToken);
        if (
            priceVersion is null ||
            priceVersion.DepartureId != candidate.DepartureId ||
            !string.Equals(priceVersion.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
            return NotFound(log, http, startedAt, timeProvider);

        var publishedPrices = await pricing.PublishedOccupancyPrices.AsNoTracking()
            .Where(item => item.PriceVersionId == priceVersion.Id && item.Amount > 0)
            .ToListAsync(cancellationToken);
        if (publishedPrices.Count == 0)
            return NotFound(log, http, startedAt, timeProvider);

        var inventoryConfiguration = await inventory.Configurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DepartureId == candidate.DepartureId, cancellationToken);
        if (
            inventoryConfiguration is null ||
            !string.Equals(inventoryConfiguration.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
            return NotFound(log, http, startedAt, timeProvider);

        var pools = await inventory.Pools.AsNoTracking()
            .Where(item => item.InventoryConfigurationId == inventoryConfiguration.Id)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var availableByPool = await InventoryAvailability.GetAvailableQuantitiesAsync(
            inventory,
            pools,
            now,
            cancellationToken);
        var availabilityByOccupancy = pools.ToDictionary(
            item => OccupancyKey(item.Occupancy.ToString()),
            item => availableByPool.GetValueOrDefault(item.Id),
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
                    availableQuantity > 0 ? "available" : "unavailable",
                    BuildFinancialPreview(
                        item.Amount,
                        occupancy,
                        candidate.DepartureDate,
                        now,
                        priceVersion.PaymentPlan));
            })
            .ToArray();
        if (!occupancies.Any(item => item.Status == "available"))
            return NotFound(log, http, startedAt, timeProvider);

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

        var travelDates = await LoadTravelDatesAsync(
            candidate,
            catalogue,
            pricing,
            inventory,
            now,
            cancellationToken);

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
            travelDates,
            new PublishedPricing(priceVersion.Currency, occupancies));

        log.LogInformation(
            "Public package details outcome={Outcome} durationMs={DurationMs} correlationId={CorrelationId}",
            "success",
            (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds,
            http.TraceIdentifier);
        return Results.Ok(response);
    }

    private static FinancialPreview BuildFinancialPreview(
        decimal unitPrice,
        string occupancy,
        DateOnly departureDate,
        DateTimeOffset now,
        PaymentPlanDefinition? paymentPlan)
    {
        var adultGuests = TravellerCountFor(occupancy);
        var total = decimal.Round(unitPrice * adultGuests, 2, MidpointRounding.ToEven);
        var schedule = QuoteScheduleCalculator.Calculate(total, departureDate, now, paymentPlan);
        var instalments = schedule.Instalments
            .Select(item => new PaymentInstalment(item.Sequence, item.DueDate, item.Amount))
            .ToArray();
        var finalDueDate = instalments.Length == 0 ? (DateOnly?)null : instalments[^1].DueDate;
        return new FinancialPreview(
            adultGuests,
            schedule.Total,
            schedule.DueNow,
            schedule.Remaining,
            instalments,
            finalDueDate);
    }

    private static async Task<IReadOnlyList<TravelDateOption>> LoadTravelDatesAsync(
        CatalogueCandidate current,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var candidates = await (
            from departure in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on departure.PackageVersionId equals packageVersion.Id
            where
                departure.Status == CatalogueDraftStatus.Published &&
                packageVersion.Status == CatalogueDraftStatus.Published &&
                departure.PublishedPriceVersionId != null &&
                departure.OperatorId == current.OperatorId &&
                departure.Origin == current.Origin &&
                packageVersion.Name == current.PackageName
            orderby departure.DepartureDate, departure.Id
            select new TravelDateCandidate(
                departure.Id,
                departure.PublishedPriceVersionId!.Value,
                departure.DepartureDate,
                departure.ReturnDate))
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0)
            return [];

        var departureIds = candidates.Select(item => item.DepartureId).ToArray();
        var priceVersionIds = candidates.Select(item => item.PriceVersionId).ToArray();
        var priceVersions = await pricing.PriceVersions.AsNoTracking()
            .Where(item => priceVersionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var prices = await pricing.PublishedOccupancyPrices.AsNoTracking()
            .Where(item => priceVersionIds.Contains(item.PriceVersionId) && item.Amount > 0)
            .ToListAsync(cancellationToken);
        var pricesByVersion = prices
            .GroupBy(item => item.PriceVersionId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var configurations = await inventory.Configurations.AsNoTracking()
            .Where(item => departureIds.Contains(item.DepartureId))
            .ToListAsync(cancellationToken);
        var configurationsByDeparture = configurations
            .GroupBy(item => item.DepartureId)
            .ToDictionary(group => group.Key, group => group.Single());
        var configurationIds = configurations.Select(item => item.Id).ToArray();
        var pools = await inventory.Pools.AsNoTracking()
            .Where(item => configurationIds.Contains(item.InventoryConfigurationId))
            .ToListAsync(cancellationToken);
        var poolsByConfiguration = pools
            .GroupBy(item => item.InventoryConfigurationId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var availableByPool = await InventoryAvailability.GetAvailableQuantitiesAsync(
            inventory,
            pools,
            now,
            cancellationToken);

        var result = new List<TravelDateOption>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (!priceVersions.TryGetValue(candidate.PriceVersionId, out var priceVersion))
                continue;
            if (
                priceVersion.DepartureId != candidate.DepartureId ||
                !string.Equals(priceVersion.OperatorId, current.OperatorId, StringComparison.Ordinal))
                continue;
            if (!pricesByVersion.TryGetValue(candidate.PriceVersionId, out var publishedPrices))
                continue;
            if (!configurationsByDeparture.TryGetValue(candidate.DepartureId, out var configuration))
                continue;
            if (!string.Equals(configuration.OperatorId, current.OperatorId, StringComparison.Ordinal))
                continue;
            if (!poolsByConfiguration.TryGetValue(configuration.Id, out var candidatePools))
                continue;

            var poolByOccupancy = candidatePools.ToDictionary(
                item => OccupancyKey(item.Occupancy.ToString()),
                StringComparer.Ordinal);
            var available = publishedPrices.Any(price =>
            {
                var occupancy = OccupancyKey(price.Occupancy.ToString());
                return poolByOccupancy.TryGetValue(occupancy, out var pool) &&
                    availableByPool.GetValueOrDefault(pool.Id) > 0;
            });
            result.Add(new TravelDateOption(
                candidate.DepartureId,
                candidate.DepartureDate,
                candidate.ReturnDate,
                available ? "available" : "sold-out"));
        }

        return result;
    }

    private static IResult NotFound(
        ILogger<Program> log,
        HttpContext http,
        DateTimeOffset startedAt,
        TimeProvider timeProvider)
    {
        log.LogInformation(
            "Public package details outcome={Outcome} durationMs={DurationMs} correlationId={CorrelationId}",
            "not_found",
            (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds,
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

    private static int TravellerCountFor(string occupancy) => occupancy switch
    {
        "double" => 2,
        "triple" => 3,
        "quad" => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
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

    private sealed record TravelDateCandidate(
        Guid DepartureId,
        Guid PriceVersionId,
        DateOnly DepartureDate,
        DateOnly ReturnDate);

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
        IReadOnlyList<TravelDateOption> TravelDates,
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

    private sealed record TravelDateOption(
        Guid DepartureId,
        DateOnly DepartureDate,
        DateOnly ReturnDate,
        string Status);

    private sealed record PublishedPricing(
        string Currency,
        IReadOnlyList<OccupancyDetail> Occupancies);

    private sealed record OccupancyDetail(
        string Occupancy,
        decimal Amount,
        int AvailableQuantity,
        string Status,
        FinancialPreview Financials);

    private sealed record FinancialPreview(
        int AdultGuests,
        decimal Total,
        decimal DueNow,
        decimal Remaining,
        IReadOnlyList<PaymentInstalment> Instalments,
        DateOnly? FinalDueDate);

    private sealed record PaymentInstalment(int Sequence, DateOnly DueDate, decimal Amount);
}