using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;

public static class InventoryHoldEndpoints
{
    private const string IdempotencyHeader = "Idempotency-Key";

    public static void MapInventoryHolds(this WebApplication app)
    {
        app.MapPost("/api/v1/quotes/{quoteId:guid}/holds", CreateHoldAsync)
            .RequireAuthorization();
        app.MapGet("/api/v1/inventory-holds/{holdId:guid}", GetHoldAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/inventory-holds/{holdId:guid}/release", ReleaseHoldAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateHoldAsync(
        Guid quoteId,
        HttpContext http,
        CatalogueDbContext catalogue,
        OperatorsDbContext operators,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        TimeProvider timeProvider,
        IOptions<InventoryHoldOptions> options,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        if (!TryReadIdempotencyKey(http, out var idempotencyKey, out var idempotencyError))
            return idempotencyError!;

        var accountId = principal.AccountId.Value;
        var idempotencyKeyHash = Hash(idempotencyKey!);
        var requestFingerprint = Hash($"{accountId}\n{quoteId:D}");

        var quote = await pricing.Quotes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == quoteId && item.AccountId == accountId, cancellationToken);
        if (quote is null)
            return Results.NotFound();

        var initialNow = timeProvider.GetUtcNow();
        if (initialNow >= quote.ExpiresAtUtc)
            return QuoteExpired(http);

        if (!await IsQuoteSaleableAsync(quote, catalogue, operators, pricing, cancellationToken))
            return HoldUnavailable(http);

        var inventoryConfiguration = await inventory.Configurations.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.DepartureId == quote.DepartureId &&
                item.OperatorId == quote.OperatorId,
                cancellationToken);
        if (inventoryConfiguration is null)
            return HoldUnavailable(http);

        var inventoryOccupancy = ToInventoryOccupancy(quote.Occupancy);
        var targetPoolId = await inventory.Pools.AsNoTracking()
            .Where(item =>
                item.InventoryConfigurationId == inventoryConfiguration.Id &&
                item.Occupancy == inventoryOccupancy)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (targetPoolId is null)
            return HoldUnavailable(http);

        await using var transaction = await inventory.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var pool = await inventory.Pools
            .FromSqlInterpolated($"SELECT * FROM inventory.inventory_pools WHERE \"Id\" = {targetPoolId.Value} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (pool is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return HoldUnavailable(http);
        }

        var now = timeProvider.GetUtcNow();
        if (now >= quote.ExpiresAtUtc)
        {
            await transaction.RollbackAsync(cancellationToken);
            return QuoteExpired(http);
        }

        await InventoryAvailability.MaterializeExpiredAsync(
            inventory,
            now,
            cancellationToken,
            pool.Id);

        var existingByKey = await inventory.Holds
            .SingleOrDefaultAsync(item =>
                item.AccountId == accountId &&
                item.IdempotencyKeyHash == idempotencyKeyHash,
                cancellationToken);
        if (existingByKey is not null)
        {
            if (!string.Equals(
                    existingByKey.RequestFingerprint,
                    requestFingerprint,
                    StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return IdempotencyConflict(http);
            }

            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(ToResponse(existingByKey, now));
        }

        var activeHold = await inventory.Holds.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.AccountId == accountId &&
                item.DepartureId == quote.DepartureId &&
                item.Occupancy == inventoryOccupancy &&
                item.State == InventoryHoldState.Active &&
                item.ExpiresAtUtc > now,
                cancellationToken);
        if (activeHold is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ActiveHoldExists(http, activeHold.Id);
        }

        var committedQuantity = await InventoryAvailability.GetCommittedQuantityAsync(
            inventory,
            pool.Id,
            now,
            cancellationToken);
        if (pool.Capacity - committedQuantity < 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return HoldUnavailable(http);
        }

        var holdLifetime = options.Value.Lifetime;
        DateTimeOffset expiresAtUtc;
        try
        {
            expiresAtUtc = InventoryHoldPolicy.CalculateExpiry(now, quote.ExpiresAtUtc, holdLifetime);
        }
        catch (ArgumentException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return QuoteExpired(http);
        }

        var hold = new InventoryHoldRecord
        {
            Id = Guid.NewGuid(),
            InventoryPoolId = pool.Id,
            DepartureId = quote.DepartureId,
            OperatorId = quote.OperatorId,
            QuoteId = quote.Id,
            AccountId = accountId,
            Occupancy = inventoryOccupancy,
            Quantity = 1,
            State = InventoryHoldState.Active,
            IdempotencyKeyHash = idempotencyKeyHash,
            RequestFingerprint = requestFingerprint,
            CorrelationId = http.TraceIdentifier,
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc
        };

        inventory.Holds.Add(hold);

        try
        {
            await inventory.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            inventory.ChangeTracker.Clear();

            var racedByKey = await inventory.Holds.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.AccountId == accountId &&
                    item.IdempotencyKeyHash == idempotencyKeyHash,
                    cancellationToken);
            if (racedByKey is not null)
                return string.Equals(racedByKey.RequestFingerprint, requestFingerprint, StringComparison.Ordinal)
                    ? Results.Ok(ToResponse(racedByKey, timeProvider.GetUtcNow()))
                    : IdempotencyConflict(http);

            var racedActive = await inventory.Holds.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.AccountId == accountId &&
                    item.DepartureId == quote.DepartureId &&
                    item.Occupancy == inventoryOccupancy &&
                    item.State == InventoryHoldState.Active &&
                    item.ExpiresAtUtc > timeProvider.GetUtcNow(),
                    cancellationToken);
            return racedActive is not null
                ? ActiveHoldExists(http, racedActive.Id)
                : HoldUnavailable(http);
        }

        log.LogInformation(
            "Inventory hold outcome={Outcome} holdId={HoldId} quoteId={QuoteId} departureId={DepartureId} occupancy={Occupancy} quantity={Quantity} durationMs={DurationMs} correlationId={CorrelationId}",
            "created",
            hold.Id,
            hold.QuoteId,
            hold.DepartureId,
            OccupancyKey(hold.Occupancy),
            hold.Quantity,
            (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds,
            http.TraceIdentifier);

        return Results.Created($"/api/v1/inventory-holds/{hold.Id}", ToResponse(hold, now));
    }

    private static async Task<IResult> GetHoldAsync(
        Guid holdId,
        HttpContext http,
        InventoryDbContext inventory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        var hold = await inventory.Holds
            .SingleOrDefaultAsync(item =>
                item.Id == holdId &&
                item.AccountId == principal.AccountId.Value,
                cancellationToken);
        if (hold is null)
            return Results.NotFound();

        var now = timeProvider.GetUtcNow();
        if (hold.State == InventoryHoldState.Active && hold.ExpiresAtUtc <= now)
        {
            hold.State = InventoryHoldState.Expired;
            hold.TerminalAtUtc = now;
            await inventory.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(ToResponse(hold, now));
    }

    private static async Task<IResult> ReleaseHoldAsync(
        Guid holdId,
        HttpContext http,
        InventoryDbContext inventory,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        await using var transaction = await inventory.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var accountId = principal.AccountId.Value;
        var hold = await inventory.Holds
            .FromSqlInterpolated($"SELECT * FROM inventory.inventory_holds WHERE \"Id\" = {holdId} AND \"AccountId\" = {accountId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (hold is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.NotFound();
        }

        var now = timeProvider.GetUtcNow();
        var previousState = hold.State;
        if (hold.State == InventoryHoldState.Active)
        {
            hold.State = hold.ExpiresAtUtc <= now
                ? InventoryHoldState.Expired
                : InventoryHoldState.Released;
            hold.TerminalAtUtc = now;
            await inventory.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        if (previousState != hold.State)
        {
            log.LogInformation(
                "Inventory hold outcome={Outcome} holdId={HoldId} quoteId={QuoteId} departureId={DepartureId} occupancy={Occupancy} quantity={Quantity} correlationId={CorrelationId}",
                hold.State == InventoryHoldState.Released ? "released" : "expired",
                hold.Id,
                hold.QuoteId,
                hold.DepartureId,
                OccupancyKey(hold.Occupancy),
                hold.Quantity,
                http.TraceIdentifier);
        }

        return Results.Ok(ToResponse(hold, now));
    }

    private static async Task<bool> IsQuoteSaleableAsync(
        QuoteRecord quote,
        CatalogueDbContext catalogue,
        OperatorsDbContext operators,
        PricingDbContext pricing,
        CancellationToken cancellationToken)
    {
        var departure = await catalogue.DepartureBatches.AsNoTracking()
            .Where(item =>
                item.Id == quote.DepartureId &&
                item.OperatorId == quote.OperatorId &&
                item.Status == CatalogueDraftStatus.Published &&
                item.PublishedPriceVersionId == quote.PriceVersionId)
            .Select(item => new { item.PackageVersionId })
            .SingleOrDefaultAsync(cancellationToken);
        if (departure is null)
            return false;

        var packagePublished = await catalogue.PackageVersions.AsNoTracking()
            .AnyAsync(item =>
                item.Id == departure.PackageVersionId &&
                item.Status == CatalogueDraftStatus.Published,
                cancellationToken);
        if (!packagePublished)
            return false;

        var operatorEligible = await operators.Operators.AsNoTracking()
            .AnyAsync(item =>
                item.Id == quote.OperatorId &&
                item.State == OperatorState.Approved,
                cancellationToken);
        if (!operatorEligible)
            return false;

        return await pricing.PriceVersions.AsNoTracking()
            .AnyAsync(item =>
                item.Id == quote.PriceVersionId &&
                item.DepartureId == quote.DepartureId &&
                item.OperatorId == quote.OperatorId,
                cancellationToken);
    }

    private static bool TryReadIdempotencyKey(
        HttpContext http,
        out string? value,
        out IResult? error)
    {
        value = http.Request.Headers[IdempotencyHeader].ToString().Trim();
        error = null;

        if (value.Length == 0)
        {
            error = Results.Problem(
                statusCode: 400,
                title: "Idempotency key required",
                extensions: ProblemExtensions(http, "idempotency_key_required"));
            return false;
        }

        if (value.Length is < 8 or > 100 || value.Any(character => character is < '!' or > '~'))
        {
            error = Results.Problem(
                statusCode: 400,
                title: "Invalid idempotency key",
                detail: "Use 8 to 100 printable ASCII characters without spaces.",
                extensions: ProblemExtensions(http, "invalid_idempotency_key"));
            return false;
        }

        return true;
    }

    private static object ToResponse(InventoryHoldRecord hold, DateTimeOffset nowUtc)
    {
        var effectiveState = InventoryHoldPolicy.EffectiveState(hold.State, hold.ExpiresAtUtc, nowUtc);
        return new
        {
            holdId = hold.Id,
            quoteId = hold.QuoteId,
            departureId = hold.DepartureId,
            occupancy = OccupancyKey(hold.Occupancy),
            quantity = hold.Quantity,
            status = effectiveState.ToString().ToLowerInvariant(),
            hold.CreatedAtUtc,
            hold.ExpiresAtUtc,
            hold.TerminalAtUtc,
            availabilityReserved = effectiveState == InventoryHoldState.Active
        };
    }

    private static InventoryOccupancy ToInventoryOccupancy(PricingOccupancy occupancy) => occupancy switch
    {
        PricingOccupancy.Double => InventoryOccupancy.Double,
        PricingOccupancy.Triple => InventoryOccupancy.Triple,
        PricingOccupancy.Quad => InventoryOccupancy.Quad,
        _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
    };

    private static string OccupancyKey(InventoryOccupancy occupancy) =>
        occupancy.ToString().ToLowerInvariant();

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static IResult NotAuthenticated(HttpContext http) => Results.Problem(
        statusCode: 401,
        title: "Sign in required",
        extensions: ProblemExtensions(http, "not_authenticated"));

    private static IResult QuoteExpired(HttpContext http) => Results.Problem(
        statusCode: 410,
        title: "Quote expired",
        detail: "Create a fresh quote before securing availability.",
        extensions: ProblemExtensions(http, "quote_expired"));

    private static IResult HoldUnavailable(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Availability could not be secured",
        detail: "This room-sharing option is no longer available for a new hold. Review the latest package options and try again.",
        extensions: ProblemExtensions(http, "hold_unavailable"));

    private static IResult ActiveHoldExists(HttpContext http, Guid holdId) => Results.Problem(
        statusCode: 409,
        title: "Availability is already secured",
        detail: "Release or continue with the existing hold before starting another one for this room selection.",
        extensions: new Dictionary<string, object?>(ProblemExtensions(http, "active_hold_exists"))
        {
            ["holdId"] = holdId
        });

    private static IResult IdempotencyConflict(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Idempotency key conflict",
        detail: "This idempotency key was already used for another hold request.",
        extensions: ProblemExtensions(http, "idempotency_conflict"));

    private static Dictionary<string, object?> ProblemExtensions(HttpContext http, string code) => new()
    {
        ["code"] = code,
        ["correlationId"] = http.TraceIdentifier
    };
}
