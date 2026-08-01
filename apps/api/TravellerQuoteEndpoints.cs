using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Traveller;
using NoorPath.Traveller.Infrastructure;

public static class TravellerQuoteEndpoints
{
    private static readonly TimeSpan QuoteLifetime = TimeSpan.FromMinutes(30);

    public static void MapTravellerQuotes(this WebApplication app)
    {
        var travellers = app.MapGroup("/api/v1/travellers").RequireAuthorization();
        travellers.MapGet("", ListTravellersAsync);
        travellers.MapPost("", CreateTravellerAsync);

        app.MapPost("/api/v1/departures/{departureId:guid}/quotes", CreateQuoteAsync)
            .RequireAuthorization();
        app.MapGet("/api/v1/quotes/{quoteId:guid}", GetQuoteAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> ListTravellersAsync(
        HttpContext http,
        TravellerDbContext travellers,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        var items = await travellers.Travellers.AsNoTracking()
            .Where(item => item.OwnerAccountId == principal.AccountId.Value)
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new TravellerResponse(item.Id, item.FullName, item.DateOfBirth))
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateTravellerAsync(
        CreateTravellerRequest request,
        HttpContext http,
        TravellerDbContext travellers,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        if (!DateOnly.TryParse(request.DateOfBirth, out var dateOfBirth))
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["dateOfBirth"] = ["Enter a valid date of birth."]
                },
                statusCode: 422,
                title: "Review traveller details");

        TravellerProfile profile;
        try
        {
            profile = new(new(request.FullName ?? string.Empty, dateOfBirth));
        }
        catch (TravellerValidationException exception)
        {
            return Results.ValidationProblem(
                exception.Errors,
                statusCode: 422,
                title: "Review traveller details");
        }

        var now = DateTimeOffset.UtcNow;
        var record = new TravellerRecord
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = principal.AccountId.Value,
            FullName = profile.Details.FullName,
            DateOfBirth = profile.Details.DateOfBirth,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        travellers.Travellers.Add(record);
        await travellers.SaveChangesAsync(cancellationToken);

        log.LogInformation(
            "Traveller profile outcome={Outcome} travellerId={TravellerId} correlationId={CorrelationId}",
            "created",
            record.Id,
            http.TraceIdentifier);

        return Results.Created(
            $"/api/v1/travellers/{record.Id}",
            new TravellerResponse(record.Id, record.FullName, record.DateOfBirth));
    }

    private static async Task<IResult> CreateQuoteAsync(
        Guid departureId,
        CreateQuoteRequest request,
        HttpContext http,
        CatalogueDbContext catalogue,
        OperatorsDbContext operators,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        TravellerDbContext travellers,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        if (!Enum.TryParse<PricingOccupancy>(request.Occupancy, true, out var occupancy))
            return QuoteValidation("occupancy", "Choose double, triple or quad sharing.");

        var travellerIds = request.TravellerIds?.ToArray() ?? [];
        var requiredTravellerCount = TravellerCountFor(occupancy);
        if (travellerIds.Length != requiredTravellerCount)
            return QuoteValidation(
                "travellerIds",
                $"{OccupancyKey(occupancy)} sharing requires exactly {requiredTravellerCount} travellers.");

        if (travellerIds.Distinct().Count() != travellerIds.Length)
            return QuoteValidation("travellerIds", "Each traveller can be included only once.");

        var candidate = await catalogue.DepartureBatches.AsNoTracking()
            .Where(item =>
                item.Id == departureId &&
                item.Status == CatalogueDraftStatus.Published &&
                item.PublishedPriceVersionId != null)
            .Select(item => new
            {
                item.Id,
                item.OperatorId,
                PriceVersionId = item.PublishedPriceVersionId!.Value,
                item.PackageVersionId,
                item.DepartureDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (candidate is null)
            return PublishedDepartureNotFound();

        var packageIsPublished = await catalogue.PackageVersions.AsNoTracking()
            .AnyAsync(item =>
                item.Id == candidate.PackageVersionId &&
                item.Status == CatalogueDraftStatus.Published,
                cancellationToken);
        if (!packageIsPublished)
            return PublishedDepartureNotFound();

        var operatorEligible = await operators.Operators.AsNoTracking()
            .AnyAsync(item =>
                item.Id == candidate.OperatorId &&
                item.State == OperatorState.Approved,
                cancellationToken);
        if (!operatorEligible)
            return PublishedDepartureNotFound();

        var priceVersion = await pricing.PriceVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == candidate.PriceVersionId, cancellationToken);
        if (priceVersion is null ||
            priceVersion.DepartureId != departureId ||
            !string.Equals(priceVersion.OperatorId, candidate.OperatorId, StringComparison.Ordinal))
            return PublishedDepartureNotFound();

        var publishedPrice = await pricing.PublishedOccupancyPrices.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.PriceVersionId == priceVersion.Id &&
                item.Occupancy == occupancy &&
                item.Amount > 0,
                cancellationToken);
        if (publishedPrice is null)
            return QuoteValidation("occupancy", "This room-sharing option is not published for sale.");

        var inventoryConfiguration = await inventory.Configurations.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.DepartureId == departureId &&
                item.OperatorId == candidate.OperatorId,
                cancellationToken);
        if (inventoryConfiguration is null)
            return QuoteUnavailable(http);

        var inventoryOccupancy = ToInventoryOccupancy(occupancy);
        var pool = await inventory.Pools.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.InventoryConfigurationId == inventoryConfiguration.Id &&
                item.Occupancy == inventoryOccupancy,
                cancellationToken);
        if (pool is null)
            return QuoteUnavailable(http);

        var availableByPool = await InventoryAvailability.GetAvailableQuantitiesAsync(
            inventory,
            [pool],
            timeProvider.GetUtcNow(),
            cancellationToken);
        if (availableByPool.GetValueOrDefault(pool.Id) < 1)
            return QuoteUnavailable(http);

        var selectedTravellers = await travellers.Travellers.AsNoTracking()
            .Where(item =>
                item.OwnerAccountId == principal.AccountId.Value &&
                travellerIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        if (selectedTravellers.Count != travellerIds.Length)
            return Results.NotFound();

        var underAge = selectedTravellers.FirstOrDefault(item =>
            item.DateOfBirth.AddYears(18) > candidate.DepartureDate);
        if (underAge is not null)
            return QuoteValidation(
                "travellerIds",
                "VS-07 currently supports adult travellers aged 18 or older on departure day.");

        var now = timeProvider.GetUtcNow();
        var total = decimal.Round(
            publishedPrice.Amount * travellerIds.Length,
            2,
            MidpointRounding.ToEven);
        var financials = QuoteScheduleCalculator.Calculate(
            total,
            candidate.DepartureDate,
            now,
            priceVersion.PaymentPlan);

        var quote = new QuoteRecord
        {
            Id = Guid.NewGuid(),
            AccountId = principal.AccountId.Value,
            DepartureId = departureId,
            OperatorId = candidate.OperatorId,
            PriceVersionId = priceVersion.Id,
            Occupancy = occupancy,
            TravellerCount = travellerIds.Length,
            Currency = priceVersion.Currency,
            UnitPrice = publishedPrice.Amount,
            Total = financials.Total,
            DueNow = financials.DueNow,
            Remaining = financials.Remaining,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(QuoteLifetime)
        };

        pricing.Quotes.Add(quote);
        pricing.QuoteTravellers.AddRange(travellerIds.Select((travellerId, index) =>
            new QuoteTravellerRecord
            {
                Id = Guid.NewGuid(),
                QuoteId = quote.Id,
                TravellerId = travellerId,
                Position = index + 1
            }));
        pricing.QuoteInstalments.AddRange(financials.Instalments.Select(item =>
            new QuoteInstalmentRecord
            {
                Id = Guid.NewGuid(),
                QuoteId = quote.Id,
                Sequence = item.Sequence,
                DueDate = item.DueDate,
                Amount = item.Amount
            }));
        await pricing.SaveChangesAsync(cancellationToken);

        log.LogInformation(
            "Quote outcome={Outcome} departureId={DepartureId} quoteId={QuoteId} occupancy={Occupancy} travellerCount={TravellerCount} priceVersionId={PriceVersionId} durationMs={DurationMs} correlationId={CorrelationId}",
            "created",
            departureId,
            quote.Id,
            OccupancyKey(occupancy),
            travellerIds.Length,
            priceVersion.Id,
            (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds,
            http.TraceIdentifier);

        return Results.Created(
            $"/api/v1/quotes/{quote.Id}",
            ToQuoteResponse(quote, financials.Instalments, expired: false));
    }

    private static async Task<IResult> GetQuoteAsync(
        Guid quoteId,
        HttpContext http,
        PricingDbContext pricing,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        var quote = await pricing.Quotes.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == quoteId &&
                item.AccountId == principal.AccountId.Value,
                cancellationToken);
        if (quote is null)
            return Results.NotFound();

        var instalments = await pricing.QuoteInstalments.AsNoTracking()
            .Where(item => item.QuoteId == quote.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new QuoteInstalment(item.Sequence, item.DueDate, item.Amount))
            .ToListAsync(cancellationToken);

        return Results.Ok(ToQuoteResponse(
            quote,
            instalments,
            expired: timeProvider.GetUtcNow() >= quote.ExpiresAtUtc));
    }

    private static object ToQuoteResponse(
        QuoteRecord quote,
        IReadOnlyList<QuoteInstalment> instalments,
        bool expired) => new
        {
            quoteId = quote.Id,
            departureId = quote.DepartureId,
            priceVersionId = quote.PriceVersionId,
            occupancy = OccupancyKey(quote.Occupancy),
            travellerCount = quote.TravellerCount,
            currency = quote.Currency,
            unitPrice = quote.UnitPrice,
            total = quote.Total,
            dueNow = quote.DueNow,
            remaining = quote.Remaining,
            instalments = instalments.Select(item => new
            {
                item.Sequence,
                item.DueDate,
                item.Amount
            }),
            quote.CreatedAtUtc,
            quote.ExpiresAtUtc,
            expired,
            availabilityReserved = false
        };

    private static int TravellerCountFor(PricingOccupancy occupancy) => occupancy switch
    {
        PricingOccupancy.Double => 2,
        PricingOccupancy.Triple => 3,
        PricingOccupancy.Quad => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
    };

    private static InventoryOccupancy ToInventoryOccupancy(PricingOccupancy occupancy) => occupancy switch
    {
        PricingOccupancy.Double => InventoryOccupancy.Double,
        PricingOccupancy.Triple => InventoryOccupancy.Triple,
        PricingOccupancy.Quad => InventoryOccupancy.Quad,
        _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
    };

    private static string OccupancyKey(PricingOccupancy occupancy) =>
        occupancy.ToString().ToLowerInvariant();

    private static IResult QuoteValidation(string field, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [field] = [message] },
            statusCode: 422,
            title: "Review your Umrah plan");

    private static IResult NotAuthenticated(HttpContext http) => Results.Problem(
        statusCode: 401,
        title: "Sign in required",
        extensions: ProblemExtensions(http, "not_authenticated"));

    private static IResult PublishedDepartureNotFound() => Results.Problem(
        statusCode: 404,
        title: "Published package not found");

    private static IResult QuoteUnavailable(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Quote unavailable",
        detail: "This room-sharing option is not currently available. Review the latest package options and try again.",
        extensions: ProblemExtensions(http, "quote_unavailable"));

    private static Dictionary<string, object?> ProblemExtensions(HttpContext http, string code) => new()
    {
        ["code"] = code,
        ["correlationId"] = http.TraceIdentifier
    };

    private sealed record TravellerResponse(Guid TravellerId, string FullName, DateOnly DateOfBirth);
}

public sealed record CreateTravellerRequest(string? FullName, string? DateOfBirth);

public sealed record CreateQuoteRequest(
    string? Occupancy,
    IReadOnlyList<Guid>? TravellerIds);
