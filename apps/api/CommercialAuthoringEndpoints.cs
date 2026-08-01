using System.Data;
using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;

public static class CommercialAuthoringEndpoints
{
    public static void MapCommercialAuthoring(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/operator/departures/{departureId:guid}").RequireAuthorization();

        group.MapGet("/commercial", GetAsync);
        group.MapPut("/pricing", SavePricingAsync);
        group.MapPut("/inventory", SaveInventoryAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid departureId,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        if (!await OwnsDraftDepartureAsync(
                departureId,
                authorization.Access!.OperatorId,
                catalogue,
                cancellationToken))
            return Results.NotFound();

        var pricePlan = await pricing.PricePlans.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DepartureId == departureId && x.OperatorId == authorization.Access.OperatorId,
                cancellationToken);
        var priceRows = pricePlan is null
            ? []
            : await pricing.OccupancyPrices.AsNoTracking()
                .Where(x => x.PricePlanId == pricePlan.Id)
                .OrderBy(x => x.Occupancy)
                .ToListAsync(cancellationToken);

        var inventoryConfiguration = await inventory.Configurations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DepartureId == departureId && x.OperatorId == authorization.Access.OperatorId,
                cancellationToken);
        var inventoryRows = inventoryConfiguration is null
            ? []
            : await inventory.Pools.AsNoTracking()
                .Where(x => x.InventoryConfigurationId == inventoryConfiguration.Id)
                .OrderBy(x => x.Occupancy)
                .ToListAsync(cancellationToken);
        var availableByPool = await InventoryAvailability.GetAvailableQuantitiesAsync(
            inventory,
            inventoryRows,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return Results.Ok(ToCommercialResponse(
            departureId,
            pricePlan,
            priceRows,
            inventoryConfiguration,
            inventoryRows,
            availableByPool));
    }

    private static async Task<IResult> SavePricingAsync(
        Guid departureId,
        SavePricingRequest request,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        if (!await OwnsDraftDepartureAsync(
                departureId,
                authorization.Access!.OperatorId,
                catalogue,
                cancellationToken))
            return Results.NotFound();

        if (!TryBuildPricing(request, out var draft, out var errors))
            return Results.ValidationProblem(errors, statusCode: 422, title: "Review pricing details");

        var plan = await pricing.PricePlans
            .SingleOrDefaultAsync(
                x => x.DepartureId == departureId && x.OperatorId == authorization.Access.OperatorId,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var action = "updated";

        if (plan is null)
        {
            if (request.ExpectedVersion != 0)
                return StalePricingVersion(http);

            plan = new PricePlanRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = authorization.Access.OperatorId,
                Currency = draft!.Details.Currency,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            pricing.PricePlans.Add(plan);
            action = "created";
        }
        else
        {
            if (plan.Version != request.ExpectedVersion)
                return StalePricingVersion(http);

            var existing = await pricing.OccupancyPrices
                .Where(x => x.PricePlanId == plan.Id)
                .ToListAsync(cancellationToken);
            pricing.OccupancyPrices.RemoveRange(existing);
            plan.Currency = draft!.Details.Currency;
            plan.Version++;
            plan.UpdatedAtUtc = now;
        }

        pricing.OccupancyPrices.AddRange(draft!.Details.Occupancies.Select(item => new OccupancyPriceRecord
        {
            Id = Guid.NewGuid(),
            PricePlanId = plan.Id,
            Occupancy = item.Occupancy,
            Amount = item.Amount
        }));
        pricing.Audits.Add(new PricingAuditRecord
        {
            Id = Guid.NewGuid(),
            PricePlanId = plan.Id,
            DepartureId = departureId,
            ActorAccountId = authorization.Principal!.AccountId.Value,
            CorrelationId = http.TraceIdentifier,
            Action = action,
            Version = plan.Version,
            Timestamp = now
        });

        try
        {
            await pricing.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StalePricingVersion(http);
        }

        log.LogInformation(
            "Pricing draft outcome={Outcome} operatorId={OperatorId} departureId={DepartureId} version={Version} correlationId={CorrelationId}",
            action,
            authorization.Access.OperatorId,
            departureId,
            plan.Version,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            departureId,
            version = plan.Version,
            currency = plan.Currency,
            occupancies = draft.Details.Occupancies.Select(item => new
            {
                occupancy = OccupancyKey(item.Occupancy),
                amount = item.Amount
            })
        });
    }

    private static async Task<IResult> SaveInventoryAsync(
        Guid departureId,
        SaveInventoryRequest request,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext catalogue,
        InventoryDbContext inventory,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        if (!await OwnsDraftDepartureAsync(
                departureId,
                authorization.Access!.OperatorId,
                catalogue,
                cancellationToken))
            return Results.NotFound();

        if (!TryBuildInventory(request, out var draft, out var errors))
            return Results.ValidationProblem(errors, statusCode: 422, title: "Review inventory details");

        var now = timeProvider.GetUtcNow();
        var action = "updated";

        await using var transaction = await inventory.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var configuration = await inventory.Configurations
            .SingleOrDefaultAsync(
                x => x.DepartureId == departureId && x.OperatorId == authorization.Access.OperatorId,
                cancellationToken);

        if (configuration is null)
        {
            if (request.ExpectedVersion != 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StaleInventoryVersion(http);
            }

            configuration = new InventoryConfigurationRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = authorization.Access.OperatorId,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            inventory.Configurations.Add(configuration);
            inventory.Pools.AddRange(draft!.Details.Pools.Select(item => new InventoryPoolRecord
            {
                Id = Guid.NewGuid(),
                InventoryConfigurationId = configuration.Id,
                Occupancy = item.Occupancy,
                Capacity = item.Capacity
            }));
            action = "created";
        }
        else
        {
            if (configuration.Version != request.ExpectedVersion)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StaleInventoryVersion(http);
            }

            var existingPools = await inventory.Pools
                .FromSqlInterpolated($"SELECT * FROM inventory.inventory_pools WHERE \"InventoryConfigurationId\" = {configuration.Id} FOR UPDATE")
                .ToListAsync(cancellationToken);
            await InventoryAvailability.MaterializeExpiredAsync(
                inventory,
                now,
                cancellationToken);

            var requestedByOccupancy = draft!.Details.Pools.ToDictionary(item => item.Occupancy);

            foreach (var existingPool in existingPools)
            {
                var committedQuantity = await InventoryAvailability.GetCommittedQuantityAsync(
                    inventory,
                    existingPool.Id,
                    now,
                    cancellationToken);
                var requestedCapacity = requestedByOccupancy.TryGetValue(
                    existingPool.Occupancy,
                    out var requestedPool)
                    ? requestedPool.Capacity
                    : 0;

                if (requestedCapacity < committedQuantity)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return CapacityBelowCommitments(http, existingPool.Occupancy, committedQuantity);
                }

                if (requestedPool is not null)
                {
                    existingPool.Capacity = requestedPool.Capacity;
                    requestedByOccupancy.Remove(existingPool.Occupancy);
                    continue;
                }

                var hasHoldHistory = await inventory.Holds.AsNoTracking()
                    .AnyAsync(item => item.InventoryPoolId == existingPool.Id, cancellationToken);
                if (hasHoldHistory)
                    existingPool.Capacity = 0;
                else
                    inventory.Pools.Remove(existingPool);
            }

            inventory.Pools.AddRange(requestedByOccupancy.Values.Select(item => new InventoryPoolRecord
            {
                Id = Guid.NewGuid(),
                InventoryConfigurationId = configuration.Id,
                Occupancy = item.Occupancy,
                Capacity = item.Capacity
            }));

            configuration.Version++;
            configuration.UpdatedAtUtc = now;
        }

        inventory.Audits.Add(new InventoryAuditRecord
        {
            Id = Guid.NewGuid(),
            InventoryConfigurationId = configuration.Id,
            DepartureId = departureId,
            ActorAccountId = authorization.Principal!.AccountId.Value,
            CorrelationId = http.TraceIdentifier,
            Reason = draft!.Details.AdjustmentReason,
            Action = action,
            Version = configuration.Version,
            Timestamp = now
        });

        try
        {
            await inventory.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StaleInventoryVersion(http);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StaleInventoryVersion(http);
        }

        var savedPools = await inventory.Pools.AsNoTracking()
            .Where(item => item.InventoryConfigurationId == configuration.Id)
            .OrderBy(item => item.Occupancy)
            .ToListAsync(cancellationToken);
        var availableByPool = await InventoryAvailability.GetAvailableQuantitiesAsync(
            inventory,
            savedPools,
            timeProvider.GetUtcNow(),
            cancellationToken);

        log.LogInformation(
            "Inventory configuration outcome={Outcome} operatorId={OperatorId} departureId={DepartureId} version={Version} correlationId={CorrelationId}",
            action,
            authorization.Access.OperatorId,
            departureId,
            configuration.Version,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            departureId,
            version = configuration.Version,
            pools = savedPools.Select(item => new
            {
                occupancy = OccupancyKey(item.Occupancy),
                capacity = item.Capacity,
                availableQuantity = availableByPool.GetValueOrDefault(item.Id)
            })
        });
    }

    private static async Task<bool> OwnsDraftDepartureAsync(
        Guid departureId,
        string operatorId,
        CatalogueDbContext catalogue,
        CancellationToken cancellationToken) => await catalogue.DepartureBatches.AsNoTracking().AnyAsync(
            x => x.Id == departureId && x.OperatorId == operatorId && x.Status == CatalogueDraftStatus.Draft,
            cancellationToken);

    private static bool TryBuildPricing(
        SavePricingRequest request,
        out PricingDraft? draft,
        out Dictionary<string, string[]> errors)
    {
        draft = null;
        errors = new();
        var rows = new List<OccupancyPriceDraft>();

        if (request.Occupancies is null)
            errors["occupancies"] = ["At least one occupancy price is required."];
        else
        {
            for (var index = 0; index < request.Occupancies.Count; index++)
            {
                var item = request.Occupancies[index];
                if (!Enum.TryParse<PricingOccupancy>(item.Occupancy, true, out var occupancy))
                {
                    errors[$"occupancies[{index}].occupancy"] = ["Use double, triple or quad."];
                    continue;
                }

                rows.Add(new(occupancy, item.Amount));
            }
        }

        if (errors.Count != 0)
            return false;

        try
        {
            draft = new(new(request.Currency ?? string.Empty, rows));
            return true;
        }
        catch (PricingDraftValidationException exception)
        {
            errors = exception.Errors;
            return false;
        }
    }

    private static bool TryBuildInventory(
        SaveInventoryRequest request,
        out InventoryDraft? draft,
        out Dictionary<string, string[]> errors)
    {
        draft = null;
        errors = new();
        var rows = new List<InventoryPoolDraft>();

        if (request.Pools is null)
            errors["pools"] = ["At least one inventory pool is required."];
        else
        {
            for (var index = 0; index < request.Pools.Count; index++)
            {
                var item = request.Pools[index];
                if (!Enum.TryParse<InventoryOccupancy>(item.Occupancy, true, out var occupancy))
                {
                    errors[$"pools[{index}].occupancy"] = ["Use double, triple or quad."];
                    continue;
                }

                rows.Add(new(occupancy, item.Capacity));
            }
        }

        if (errors.Count != 0)
            return false;

        try
        {
            draft = new(new(request.AdjustmentReason ?? string.Empty, rows));
            return true;
        }
        catch (InventoryDraftValidationException exception)
        {
            errors = exception.Errors;
            return false;
        }
    }

    private static object ToCommercialResponse(
        Guid departureId,
        PricePlanRecord? pricePlan,
        IReadOnlyCollection<OccupancyPriceRecord> prices,
        InventoryConfigurationRecord? configuration,
        IReadOnlyCollection<InventoryPoolRecord> pools,
        IReadOnlyDictionary<Guid, int> availableByPool)
    {
        var priceByKey = prices.ToDictionary(x => OccupancyKey(x.Occupancy), StringComparer.OrdinalIgnoreCase);
        var poolByKey = pools.ToDictionary(x => OccupancyKey(x.Occupancy), StringComparer.OrdinalIgnoreCase);
        var keys = new[] { "double", "triple", "quad" };

        return new
        {
            departureId,
            pricing = pricePlan is null ? null : new
            {
                version = pricePlan.Version,
                currency = pricePlan.Currency,
                occupancies = prices.OrderBy(x => x.Occupancy).Select(item => new
                {
                    occupancy = OccupancyKey(item.Occupancy),
                    amount = item.Amount
                })
            },
            inventory = configuration is null ? null : new
            {
                version = configuration.Version,
                pools = pools.OrderBy(x => x.Occupancy).Select(item => new
                {
                    occupancy = OccupancyKey(item.Occupancy),
                    capacity = item.Capacity,
                    availableQuantity = availableByPool.GetValueOrDefault(item.Id)
                })
            },
            readiness = keys.Select(key =>
            {
                var hasPool = poolByKey.TryGetValue(key, out var pool);
                var available = hasPool ? availableByPool.GetValueOrDefault(pool!.Id) : 0;
                return new
                {
                    occupancy = key,
                    hasPrice = priceByKey.ContainsKey(key),
                    capacity = hasPool ? pool!.Capacity : (int?)null,
                    ready = priceByKey.ContainsKey(key) && available > 0
                };
            })
        };
    }

    private static async Task<OperatorAuthorization> ResolveOperatorAsync(
        HttpContext http,
        IOperatorAccess operators,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return new(null, null, Results.Problem(
                statusCode: 401,
                title: "Sign in required",
                extensions: ProblemExtensions(http, "not_authenticated")));

        var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return new(principal, access, Results.Problem(
                statusCode: 403,
                title: "Access unavailable",
                detail: "This account does not have access to operator administration.",
                extensions: ProblemExtensions(http, "forbidden")));

        return new(principal, access, null);
    }

    private static string OccupancyKey(PricingOccupancy occupancy) => occupancy.ToString().ToLowerInvariant();
    private static string OccupancyKey(InventoryOccupancy occupancy) => occupancy.ToString().ToLowerInvariant();

    private static IResult StalePricingVersion(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Pricing changed",
        detail: "Pricing changed. Reload the latest configuration before saving again.",
        extensions: ProblemExtensions(http, "stale_pricing_version"));

    private static IResult StaleInventoryVersion(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Inventory changed",
        detail: "Inventory changed. Reload the latest configuration before saving again.",
        extensions: ProblemExtensions(http, "stale_inventory_version"));

    private static IResult CapacityBelowCommitments(
        HttpContext http,
        InventoryOccupancy occupancy,
        int committedQuantity) => Results.Problem(
        statusCode: 409,
        title: "Capacity is below active commitments",
        detail: $"{OccupancyKey(occupancy)} capacity cannot be lower than {committedQuantity} while availability is secured.",
        extensions: ProblemExtensions(http, "capacity_below_commitments"));

    private static Dictionary<string, object?> ProblemExtensions(HttpContext http, string code) => new()
    {
        ["code"] = code,
        ["correlationId"] = http.TraceIdentifier
    };

    private sealed record OperatorAuthorization(
        CurrentPrincipal? Principal,
        OperatorAccess? Access,
        IResult? Error);
}

public sealed record OccupancyPriceRequest(
    string? Occupancy,
    decimal Amount);

public sealed record SavePricingRequest(
    int ExpectedVersion,
    string? Currency,
    IReadOnlyList<OccupancyPriceRequest>? Occupancies);

public sealed record InventoryPoolRequest(
    string? Occupancy,
    int Capacity);

public sealed record SaveInventoryRequest(
    int ExpectedVersion,
    string? AdjustmentReason,
    IReadOnlyList<InventoryPoolRequest>? Pools);
