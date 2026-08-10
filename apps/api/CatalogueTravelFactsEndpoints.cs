using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;

public static class CatalogueTravelFactsEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void MapCatalogueTravelFacts(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/operator/departures/{departureId:guid}/travel-facts")
            .RequireAuthorization();

        group.MapGet("", GetAsync);
        group.MapPut("", UpdateAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid departureId,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext db,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Unauthorized(http);

        var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return Forbidden(http);

        var departure = await db.DepartureBatches.AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == departureId && value.OperatorId == access.OperatorId,
                cancellationToken);
        if (departure is null)
            return Results.NotFound();

        var packageVersion = await db.PackageVersions.AsNoTracking()
            .SingleAsync(value => value.Id == departure.PackageVersionId, cancellationToken);
        var legs = Deserialize(packageVersion.TravelFactsJson);

        return Results.Ok(new
        {
            departureId,
            version = packageVersion.TravelFactsVersion,
            editable = departure.Status == CatalogueDraftStatus.Draft,
            legs = legs.Select(ToResponse)
        });
    }

    private static async Task<IResult> UpdateAsync(
        Guid departureId,
        UpdatePackageTravelFactsRequest request,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext db,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Unauthorized(http);

        var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return Forbidden(http);

        if (!TryBuild(request.Legs ?? [], out var draft, out var errors))
            return Results.ValidationProblem(errors, statusCode: 422, title: "Review the flight facts");

        var departure = await db.DepartureBatches
            .SingleOrDefaultAsync(
                value => value.Id == departureId && value.OperatorId == access.OperatorId,
                cancellationToken);
        if (departure is null)
            return Results.NotFound();

        if (departure.Status != CatalogueDraftStatus.Draft)
            return Results.Problem(
                statusCode: 409,
                title: "Travel facts are locked",
                detail: "Submitted and published departures cannot be edited.",
                extensions: ProblemExtensions(http, "travel_facts_not_editable"));

        var packageVersion = await db.PackageVersions
            .SingleAsync(value => value.Id == departure.PackageVersionId, cancellationToken);
        if (packageVersion.TravelFactsVersion != request.ExpectedVersion)
            return StaleVersion(http);

        var now = DateTimeOffset.UtcNow;
        packageVersion.TravelFactsJson = JsonSerializer.Serialize(draft!.Legs, JsonOptions);
        packageVersion.TravelFactsVersion++;
        packageVersion.UpdatedAtUtc = now;
        db.DraftAudits.Add(new CatalogueDraftAuditRecord
        {
            Id = Guid.NewGuid(),
            DepartureBatchId = departure.Id,
            ActorAccountId = principal.AccountId.Value,
            CorrelationId = http.TraceIdentifier,
            Action = "travel_facts_saved",
            Version = packageVersion.TravelFactsVersion,
            Timestamp = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StaleVersion(http);
        }

        log.LogInformation(
            "Catalogue travel facts outcome={Outcome} operatorId={OperatorId} departureId={DepartureId} version={Version} legCount={LegCount} correlationId={CorrelationId}",
            "saved",
            access.OperatorId,
            departure.Id,
            packageVersion.TravelFactsVersion,
            draft.Legs.Count,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            departureId,
            version = packageVersion.TravelFactsVersion,
            editable = true,
            legs = draft.Legs.Select(ToResponse)
        });
    }

    private static bool TryBuild(
        IReadOnlyList<FlightLegRequest> requests,
        out PackageTravelFactsDraft? draft,
        out Dictionary<string, string[]> errors)
    {
        errors = new();
        draft = null;
        var legs = new List<FlightLegDraft>(requests.Count);

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            if (!Enum.TryParse<FactConfirmationState>(request.ConfirmationState, true, out var state))
            {
                errors[$"legs[{index}].confirmationState"] = ["Use pending or confirmed."];
                continue;
            }

            legs.Add(new FlightLegDraft(
                request.AirlineName ?? string.Empty,
                request.AirlineCode ?? string.Empty,
                request.FlightNumber ?? string.Empty,
                request.DepartureAirportName ?? string.Empty,
                request.DepartureAirportCode ?? string.Empty,
                request.ArrivalAirportName ?? string.Empty,
                request.ArrivalAirportCode ?? string.Empty,
                state));
        }

        if (errors.Count != 0)
            return false;

        try
        {
            draft = new PackageTravelFactsDraft(legs);
            return true;
        }
        catch (CatalogueDraftValidationException exception)
        {
            errors = exception.Errors;
            return false;
        }
    }

    private static IReadOnlyList<FlightLegDraft> Deserialize(string json) =>
        JsonSerializer.Deserialize<FlightLegDraft[]>(json, JsonOptions) ?? [];

    private static object ToResponse(FlightLegDraft leg) => new
    {
        airlineName = leg.AirlineName,
        airlineCode = leg.AirlineCode,
        flightNumber = leg.FlightNumber,
        departureAirportName = leg.DepartureAirportName,
        departureAirportCode = leg.DepartureAirportCode,
        arrivalAirportName = leg.ArrivalAirportName,
        arrivalAirportCode = leg.ArrivalAirportCode,
        confirmationState = leg.ConfirmationState.ToString().ToLowerInvariant()
    };

    private static IResult Unauthorized(HttpContext http) => Results.Problem(
        statusCode: 401,
        title: "Sign in required",
        extensions: ProblemExtensions(http, "not_authenticated"));

    private static IResult Forbidden(HttpContext http) => Results.Problem(
        statusCode: 403,
        title: "Access unavailable",
        detail: "This account does not have access to operator administration.",
        extensions: ProblemExtensions(http, "forbidden"));

    private static IResult StaleVersion(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Travel facts changed",
        detail: "The travel facts changed. Refresh them before saving again.",
        extensions: ProblemExtensions(http, "travel_facts_stale_version"));

    private static Dictionary<string, object?> ProblemExtensions(HttpContext http, string code) => new()
    {
        ["code"] = code,
        ["correlationId"] = http.TraceIdentifier
    };
}

public sealed record FlightLegRequest(
    string? AirlineName,
    string? AirlineCode,
    string? FlightNumber,
    string? DepartureAirportName,
    string? DepartureAirportCode,
    string? ArrivalAirportName,
    string? ArrivalAirportCode,
    string? ConfirmationState);

public sealed record UpdatePackageTravelFactsRequest(
    int ExpectedVersion,
    IReadOnlyList<FlightLegRequest>? Legs);
