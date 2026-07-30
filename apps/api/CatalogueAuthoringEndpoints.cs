using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;

public static class CatalogueAuthoringEndpoints
{
    public static void MapCatalogueAuthoring(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/operator/departures").RequireAuthorization();

        group.MapPost("", CreateAsync);
        group.MapGet("/{departureId:guid}", GetAsync);
        group.MapPut("/{departureId:guid}", UpdateAsync);
    }

    private static async Task<IResult> CreateAsync(
        SaveCatalogueDraftRequest request,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext db,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        if (!TryBuildDraft(request, out var draft, out var errors))
            return Results.ValidationProblem(errors, statusCode: 422, title: "Review the draft details");

        var now = DateTimeOffset.UtcNow;
        var template = new PackageTemplateRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = authorization.Access!.OperatorId,
            WorkingName = draft!.Details.PackageName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var packageVersion = CreatePackageVersion(template.Id, draft, now);
        var departure = new DepartureBatchRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = authorization.Access.OperatorId,
            PackageVersionId = packageVersion.Id,
            Origin = draft.Details.Origin,
            DepartureDate = draft.Details.DepartureDate,
            ReturnDate = draft.Details.ReturnDate,
            Status = CatalogueDraftStatus.Draft,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.AddRange(template, packageVersion, departure);
        AddContentItems(db, packageVersion.Id, draft);
        db.DraftAudits.Add(CreateAudit(departure.Id, authorization.Principal!.AccountId.Value, http.TraceIdentifier, "created", 1, now));

        await db.SaveChangesAsync(cancellationToken);

        log.LogInformation(
            "Catalogue draft outcome={Outcome} operatorId={OperatorId} departureId={DepartureId} version={Version} correlationId={CorrelationId}",
            "created",
            authorization.Access.OperatorId,
            departure.Id,
            departure.Version,
            http.TraceIdentifier);

        return Results.Created(
            $"/api/v1/operator/departures/{departure.Id}",
            new
            {
                packageTemplateId = template.Id,
                packageVersionId = packageVersion.Id,
                departureId = departure.Id,
                version = departure.Version,
                status = "draft"
            });
    }

    private static async Task<IResult> GetAsync(
        Guid departureId,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext db,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        var departure = await db.DepartureBatches.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == departureId && x.OperatorId == authorization.Access!.OperatorId,
                cancellationToken);
        if (departure is null)
            return Results.NotFound();

        var packageVersion = await db.PackageVersions.AsNoTracking()
            .SingleAsync(x => x.Id == departure.PackageVersionId, cancellationToken);
        var template = await db.PackageTemplates.AsNoTracking()
            .SingleAsync(x => x.Id == packageVersion.PackageTemplateId, cancellationToken);
        var items = await db.PackageContentItems.AsNoTracking()
            .Where(x => x.PackageVersionId == packageVersion.Id)
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Position)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToResponse(template, packageVersion, departure, items));
    }

    private static async Task<IResult> UpdateAsync(
        Guid departureId,
        UpdateCatalogueDraftRequest request,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext db,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (authorization.Error is not null)
            return authorization.Error;

        if (!TryBuildDraft(request.Draft, out var draft, out var errors))
            return Results.ValidationProblem(errors, statusCode: 422, title: "Review the draft details");

        var departure = await db.DepartureBatches
            .SingleOrDefaultAsync(
                x => x.Id == departureId && x.OperatorId == authorization.Access!.OperatorId,
                cancellationToken);
        if (departure is null)
            return Results.NotFound();

        if (departure.Version != request.ExpectedVersion)
            return StaleVersion(http);

        var packageVersion = await db.PackageVersions
            .SingleAsync(x => x.Id == departure.PackageVersionId, cancellationToken);
        var template = await db.PackageTemplates
            .SingleAsync(x => x.Id == packageVersion.PackageTemplateId, cancellationToken);
        var existingItems = await db.PackageContentItems
            .Where(x => x.PackageVersionId == packageVersion.Id)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        template.WorkingName = draft!.Details.PackageName;
        template.UpdatedAtUtc = now;
        ApplyPackageVersion(packageVersion, draft, now);
        departure.Origin = draft.Details.Origin;
        departure.DepartureDate = draft.Details.DepartureDate;
        departure.ReturnDate = draft.Details.ReturnDate;
        departure.Version++;
        departure.UpdatedAtUtc = now;

        db.PackageContentItems.RemoveRange(existingItems);
        AddContentItems(db, packageVersion.Id, draft);
        db.DraftAudits.Add(CreateAudit(
            departure.Id,
            authorization.Principal!.AccountId.Value,
            http.TraceIdentifier,
            "updated",
            departure.Version,
            now));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StaleVersion(http);
        }

        log.LogInformation(
            "Catalogue draft outcome={Outcome} operatorId={OperatorId} departureId={DepartureId} version={Version} correlationId={CorrelationId}",
            "updated",
            authorization.Access.OperatorId,
            departure.Id,
            departure.Version,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            packageTemplateId = template.Id,
            packageVersionId = packageVersion.Id,
            departureId = departure.Id,
            version = departure.Version,
            status = "draft"
        });
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

    private static bool TryBuildDraft(
        SaveCatalogueDraftRequest request,
        out PackageDepartureDraft? draft,
        out Dictionary<string, string[]> errors)
    {
        errors = new();
        draft = null;

        var makkahState = ParseState(request.Makkah?.ConfirmationState, "makkah.confirmationState", errors);
        var madinahState = ParseState(request.Madinah?.ConfirmationState, "madinah.confirmationState", errors);
        var travelState = ParseState(request.Travel?.ConfirmationState, "travel.confirmationState", errors);

        if (request.Makkah is null)
            errors["makkah"] = ["Makkah accommodation is required."];
        if (request.Madinah is null)
            errors["madinah"] = ["Madinah accommodation is required."];
        if (request.Travel is null)
            errors["travel"] = ["Travel facts are required."];
        if (errors.Count != 0)
            return false;

        var details = new PackageDepartureDraftDetails(
            request.PackageName ?? string.Empty,
            request.Summary ?? string.Empty,
            new(
                request.Makkah!.HotelName ?? string.Empty,
                request.Makkah.Classification ?? string.Empty,
                request.Makkah.DistanceDisclosure ?? string.Empty,
                request.Makkah.Nights,
                makkahState!.Value),
            new(
                request.Madinah!.HotelName ?? string.Empty,
                request.Madinah.Classification ?? string.Empty,
                request.Madinah.DistanceDisclosure ?? string.Empty,
                request.Madinah.Nights,
                madinahState!.Value),
            new(
                request.Travel!.RouteSummary ?? string.Empty,
                request.Travel.Details ?? string.Empty,
                travelState!.Value),
            request.Origin ?? string.Empty,
            request.DepartureDate,
            request.ReturnDate,
            request.Inclusions ?? [],
            request.Exclusions ?? []);

        try
        {
            draft = new(details);
            return true;
        }
        catch (CatalogueDraftValidationException exception)
        {
            errors = exception.Errors;
            return false;
        }
    }

    private static FactConfirmationState? ParseState(
        string? value,
        string key,
        IDictionary<string, string[]> errors)
    {
        if (Enum.TryParse<FactConfirmationState>(value, true, out var state))
            return state;

        errors[key] = ["Use pending or confirmed."];
        return null;
    }

    private static PackageVersionRecord CreatePackageVersion(
        Guid templateId,
        PackageDepartureDraft draft,
        DateTimeOffset now)
    {
        var record = new PackageVersionRecord
        {
            Id = Guid.NewGuid(),
            PackageTemplateId = templateId,
            Sequence = 1,
            Status = CatalogueDraftStatus.Draft,
            Name = draft.Details.PackageName,
            Summary = draft.Details.Summary,
            MakkahHotelName = string.Empty,
            MakkahClassification = string.Empty,
            MakkahDistanceDisclosure = string.Empty,
            MadinahHotelName = string.Empty,
            MadinahClassification = string.Empty,
            MadinahDistanceDisclosure = string.Empty,
            TravelRouteSummary = string.Empty,
            TravelDetails = string.Empty,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        ApplyPackageVersion(record, draft, now);
        return record;
    }

    private static void ApplyPackageVersion(
        PackageVersionRecord record,
        PackageDepartureDraft draft,
        DateTimeOffset now)
    {
        record.Name = draft.Details.PackageName;
        record.Summary = draft.Details.Summary;
        record.MakkahHotelName = draft.Details.Makkah.HotelName;
        record.MakkahClassification = draft.Details.Makkah.Classification;
        record.MakkahDistanceDisclosure = draft.Details.Makkah.DistanceDisclosure;
        record.MakkahNights = draft.Details.Makkah.Nights;
        record.MakkahConfirmationState = draft.Details.Makkah.ConfirmationState;
        record.MadinahHotelName = draft.Details.Madinah.HotelName;
        record.MadinahClassification = draft.Details.Madinah.Classification;
        record.MadinahDistanceDisclosure = draft.Details.Madinah.DistanceDisclosure;
        record.MadinahNights = draft.Details.Madinah.Nights;
        record.MadinahConfirmationState = draft.Details.Madinah.ConfirmationState;
        record.TravelRouteSummary = draft.Details.Travel.RouteSummary;
        record.TravelDetails = draft.Details.Travel.Details;
        record.TravelConfirmationState = draft.Details.Travel.ConfirmationState;
        record.UpdatedAtUtc = now;
    }

    private static void AddContentItems(
        CatalogueDbContext db,
        Guid packageVersionId,
        PackageDepartureDraft draft)
    {
        db.PackageContentItems.AddRange(
            draft.Details.Inclusions.Select((text, position) => new PackageContentItemRecord
            {
                Id = Guid.NewGuid(),
                PackageVersionId = packageVersionId,
                Kind = PackageContentKind.Inclusion,
                Position = position,
                Text = text
            }));
        db.PackageContentItems.AddRange(
            draft.Details.Exclusions.Select((text, position) => new PackageContentItemRecord
            {
                Id = Guid.NewGuid(),
                PackageVersionId = packageVersionId,
                Kind = PackageContentKind.Exclusion,
                Position = position,
                Text = text
            }));
    }

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

    private static object ToResponse(
        PackageTemplateRecord template,
        PackageVersionRecord packageVersion,
        DepartureBatchRecord departure,
        IReadOnlyList<PackageContentItemRecord> items) => new
        {
            packageTemplateId = template.Id,
            packageVersionId = packageVersion.Id,
            departureId = departure.Id,
            version = departure.Version,
            status = "draft",
            packageName = packageVersion.Name,
            summary = packageVersion.Summary,
            makkah = new
            {
                hotelName = packageVersion.MakkahHotelName,
                classification = packageVersion.MakkahClassification,
                distanceDisclosure = packageVersion.MakkahDistanceDisclosure,
                nights = packageVersion.MakkahNights,
                confirmationState = packageVersion.MakkahConfirmationState.ToString().ToLowerInvariant()
            },
            madinah = new
            {
                hotelName = packageVersion.MadinahHotelName,
                classification = packageVersion.MadinahClassification,
                distanceDisclosure = packageVersion.MadinahDistanceDisclosure,
                nights = packageVersion.MadinahNights,
                confirmationState = packageVersion.MadinahConfirmationState.ToString().ToLowerInvariant()
            },
            travel = new
            {
                routeSummary = packageVersion.TravelRouteSummary,
                details = packageVersion.TravelDetails,
                confirmationState = packageVersion.TravelConfirmationState.ToString().ToLowerInvariant()
            },
            origin = departure.Origin,
            departureDate = departure.DepartureDate,
            returnDate = departure.ReturnDate,
            inclusions = items.Where(x => x.Kind == PackageContentKind.Inclusion).OrderBy(x => x.Position).Select(x => x.Text),
            exclusions = items.Where(x => x.Kind == PackageContentKind.Exclusion).OrderBy(x => x.Position).Select(x => x.Text)
        };

    private static IResult StaleVersion(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Draft changed",
        detail: "The draft changed. Refresh it before saving again.",
        extensions: ProblemExtensions(http, "stale_version"));

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

public sealed record AccommodationDraftRequest(
    string? HotelName,
    string? Classification,
    string? DistanceDisclosure,
    int Nights,
    string? ConfirmationState);

public sealed record TravelDraftRequest(
    string? RouteSummary,
    string? Details,
    string? ConfirmationState);

public sealed record SaveCatalogueDraftRequest(
    string? PackageName,
    string? Summary,
    AccommodationDraftRequest? Makkah,
    AccommodationDraftRequest? Madinah,
    TravelDraftRequest? Travel,
    string? Origin,
    DateOnly DepartureDate,
    DateOnly ReturnDate,
    IReadOnlyList<string>? Inclusions,
    IReadOnlyList<string>? Exclusions);

public sealed record UpdateCatalogueDraftRequest(
    int ExpectedVersion,
    SaveCatalogueDraftRequest Draft);
