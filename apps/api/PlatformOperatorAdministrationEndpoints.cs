using Microsoft.EntityFrameworkCore;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;

public static class PlatformOperatorAdministrationEndpoints
{
    private const string AdministratorConfiguration =
        "Authorization:PlatformAdministratorAccountIds";

    public static void MapPlatformOperatorAdministration(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platform/operators")
            .RequireAuthorization();

        group.MapGet("/summary", GetSummaryAsync);
        group.MapGet("", ListOperatorsAsync);
        group.MapGet("/{operatorId}", GetOperatorAsync);
        group.MapPost("/{operatorId}/state", ChangeStateAsync);
    }

    private static async Task<IResult> GetSummaryAsync(
        HttpContext http,
        IConfiguration configuration,
        OperatorsDbContext operators,
        CancellationToken cancellationToken)
    {
        var authorization = ResolveAdministrator(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        var counts = await operators.Operators
            .AsNoTracking()
            .GroupBy(item => item.State)
            .Select(group => new { State = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byState = counts.ToDictionary(item => item.State, item => item.Count);
        var pendingRecords = await operators.Operators
            .AsNoTracking()
            .Where(item => item.State == OperatorState.PendingApproval)
            .OrderBy(item => item.UpdatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            total = byState.Values.Sum(),
            pendingApproval = Count(byState, OperatorState.PendingApproval),
            approved = Count(byState, OperatorState.Approved),
            suspended = Count(byState, OperatorState.Suspended),
            rejected = Count(byState, OperatorState.Rejected),
            deactivated = Count(byState, OperatorState.Deactivated),
            pending = pendingRecords.Select(item => new
            {
                id = item.Id,
                displayName = item.DisplayName,
                state = StateKey(item.State),
                version = item.Version,
                updatedAtUtc = item.UpdatedAtUtc
            })
        });
    }

    private static async Task<IResult> ListOperatorsAsync(
        HttpContext http,
        IConfiguration configuration,
        OperatorsDbContext operators,
        string? state,
        CancellationToken cancellationToken)
    {
        var authorization = ResolveAdministrator(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        OperatorState? stateFilter = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (!Enum.TryParse<OperatorState>(state, true, out var parsedState))
                return Results.Problem(
                    statusCode: 400,
                    title: "Operator state is invalid",
                    detail: "Use a NoorPath operator lifecycle state.",
                    extensions: ProblemExtensions(http, "invalid_operator_state"));
            stateFilter = parsedState;
        }

        var query = operators.Operators.AsNoTracking();
        if (stateFilter is not null)
            query = query.Where(item => item.State == stateFilter.Value);

        var items = await query
            .OrderBy(item => item.State == OperatorState.PendingApproval ? 0 : 1)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ThenBy(item => item.DisplayName)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            items = items.Select(ToOperatorResponse)
        });
    }

    private static async Task<IResult> GetOperatorAsync(
        string operatorId,
        HttpContext http,
        IConfiguration configuration,
        OperatorsDbContext operators,
        CancellationToken cancellationToken)
    {
        var authorization = ResolveAdministrator(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        var operation = await operators.Operators
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == operatorId, cancellationToken);
        if (operation is null)
            return Results.NotFound();

        var membershipRecords = await operators.Memberships
            .AsNoTracking()
            .Where(item => item.OperatorId == operatorId)
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var historyRecords = await operators.StateAudits
            .AsNoTracking()
            .Where(item => item.OperatorId == operatorId)
            .OrderByDescending(item => item.Timestamp)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            @operator = ToOperatorResponse(operation),
            memberships = membershipRecords.Select(item => new
            {
                item.Id,
                item.AccountId,
                status = item.Status.ToString().ToLowerInvariant(),
                item.CreatedAtUtc,
                item.UpdatedAtUtc
            }),
            history = historyRecords.Select(item => new
            {
                item.Id,
                fromState = StateKey(item.FromState),
                toState = StateKey(item.ToState),
                item.ActorAccountId,
                item.Reason,
                item.OperatorVersion,
                item.Timestamp
            })
        });
    }

    private static async Task<IResult> ChangeStateAsync(
        string operatorId,
        OperatorStateChangeRequest request,
        HttpContext http,
        IConfiguration configuration,
        OperatorsDbContext operators,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var authorization = ResolveAdministrator(http, configuration);
        if (authorization.Error is not null)
            return authorization.Error;

        if (!Enum.TryParse<OperatorState>(request.TargetState, true, out var targetState))
            return Results.Problem(
                statusCode: 400,
                title: "Operator state is invalid",
                detail: "Use a NoorPath operator lifecycle state.",
                extensions: ProblemExtensions(http, "invalid_operator_state"));

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? null
            : request.Reason.Trim();
        if (reason?.Length > 500)
            return Results.Problem(
                statusCode: 422,
                title: "Decision reason is too long",
                detail: "Keep the operator decision reason within 500 characters.",
                extensions: ProblemExtensions(http, "operator_reason_too_long"));

        if (OperatorStatePolicy.RequiresReason(targetState) && reason is null)
            return Results.Problem(
                statusCode: 422,
                title: "Decision reason is required",
                detail: "Rejection, suspension and deactivation require an auditable reason.",
                extensions: ProblemExtensions(http, "operator_reason_required"));

        var operation = await operators.Operators
            .SingleOrDefaultAsync(item => item.Id == operatorId, cancellationToken);
        if (operation is null)
            return Results.NotFound();

        if (operation.Version != request.ExpectedVersion)
            return StaleDecision(http);

        if (!OperatorStatePolicy.CanTransition(operation.State, targetState))
            return Results.Problem(
                statusCode: 409,
                title: "Operator transition unavailable",
                detail: $"{StateKey(operation.State)} cannot move to {StateKey(targetState)}.",
                extensions: ProblemExtensions(http, "invalid_operator_transition"));

        var previousState = operation.State;
        var now = DateTimeOffset.UtcNow;
        operation.State = targetState;
        operation.Version++;
        operation.UpdatedAtUtc = now;
        operators.StateAudits.Add(new OperatorStateAuditRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = operation.Id,
            FromState = previousState,
            ToState = targetState,
            ActorAccountId = authorization.Principal!.AccountId.Value,
            Reason = reason,
            CorrelationId = http.TraceIdentifier,
            OperatorVersion = operation.Version,
            Timestamp = now
        });

        try
        {
            await operators.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StaleDecision(http);
        }

        log.LogInformation(
            "Platform operator lifecycle outcome={Outcome} actorAccountId={ActorAccountId} operatorId={OperatorId} fromState={FromState} toState={ToState} operatorVersion={OperatorVersion} correlationId={CorrelationId}",
            "changed",
            authorization.Principal!.AccountId,
            operation.Id,
            previousState,
            targetState,
            operation.Version,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            @operator = ToOperatorResponse(operation),
            changedAtUtc = now
        });
    }

    private static PlatformAuthorization ResolveAdministrator(
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
                    extensions: ProblemExtensions(http, "not_authenticated")));

        var administrators = configuration
            .GetSection(AdministratorConfiguration)
            .Get<string[]>() ?? [];
        if (!administrators.Contains(principal.AccountId.Value, StringComparer.Ordinal))
            return new(
                principal,
                Results.Problem(
                    statusCode: 403,
                    title: "Platform access unavailable",
                    detail: "This account is not authorized to administer NoorPath operators.",
                    extensions: ProblemExtensions(http, "forbidden")));

        return new(principal, null);
    }

    private static object ToOperatorResponse(OperatorRecord operation) => new
    {
        id = operation.Id,
        displayName = operation.DisplayName,
        state = StateKey(operation.State),
        version = operation.Version,
        createdAtUtc = operation.CreatedAtUtc,
        updatedAtUtc = operation.UpdatedAtUtc,
        allowedTransitions = Enum.GetValues<OperatorState>()
            .Where(target => OperatorStatePolicy.CanTransition(operation.State, target))
            .Select(StateKey)
    };

    private static int Count(
        IReadOnlyDictionary<OperatorState, int> counts,
        OperatorState state) => counts.TryGetValue(state, out var value) ? value : 0;

    private static string StateKey(OperatorState state) => state switch
    {
        OperatorState.PendingApproval => "pendingApproval",
        _ => char.ToLowerInvariant(state.ToString()[0]) + state.ToString()[1..]
    };

    private static IResult StaleDecision(HttpContext http) =>
        Results.Problem(
            statusCode: 409,
            title: "Operator record changed",
            detail: "Reload this operator before making another lifecycle decision.",
            extensions: ProblemExtensions(http, "stale_operator_decision"));

    private static Dictionary<string, object?> ProblemExtensions(
        HttpContext http,
        string code) => new()
        {
            ["code"] = code,
            ["correlationId"] = http.TraceIdentifier
        };

    private sealed record PlatformAuthorization(
        CurrentPrincipal? Principal,
        IResult? Error);
}

public sealed record OperatorStateChangeRequest(
    string TargetState,
    int ExpectedVersion,
    string? Reason);
