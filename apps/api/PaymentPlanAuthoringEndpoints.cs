using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;

public static class PaymentPlanAuthoringEndpoints
{
    public static void MapPaymentPlanAuthoring(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/operator/departures/{departureId:guid}")
            .RequireAuthorization();
        group.MapGet("/payment-plan", GetAsync);
        group.MapPut("/payment-plan", SaveAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid departureId,
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext catalogue,
        PricingDbContext pricing,
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
            .SingleOrDefaultAsync(item =>
                item.DepartureId == departureId &&
                item.OperatorId == authorization.Access.OperatorId,
                cancellationToken);

        if (pricePlan is null)
            return Results.Ok(new { pricingVersion = 0, paymentPlan = (object?)null });

        return Results.Ok(ToResponse(pricePlan));
    }

    private static async Task<IResult> SaveAsync(
        Guid departureId,
        SavePaymentPlanRequest request,
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

        var pricePlan = await pricing.PricePlans.SingleOrDefaultAsync(item =>
            item.DepartureId == departureId &&
            item.OperatorId == authorization.Access.OperatorId,
            cancellationToken);
        if (pricePlan is null)
            return Results.Problem(
                statusCode: 409,
                title: "Save pricing first",
                detail: "Configure occupancy pricing before adding a payment plan.",
                extensions: ProblemExtensions(http, "pricing_required"));

        if (pricePlan.Version != request.ExpectedPricingVersion)
            return StalePricingVersion(http);

        PaymentPlanDefinition? definition = null;
        if (request.Enabled)
        {
            if (request.DepositPercent is null ||
                request.InstalmentDayOfMonth is null ||
                request.FinalPaymentDueDaysBeforeDeparture is null)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["paymentPlan"] = ["Deposit percentage, instalment day and final payment deadline are required when the plan is enabled."]
                    },
                    statusCode: 422,
                    title: "Review payment plan");
            }

            definition = new(
                request.DepositPercent.Value,
                request.InstalmentDayOfMonth.Value,
                request.FinalPaymentDueDaysBeforeDeparture.Value);
            var errors = PaymentPlanPolicy.Validate(definition);
            if (errors.Count != 0)
                return Results.ValidationProblem(
                    errors,
                    statusCode: 422,
                    title: "Review payment plan");
        }

        var changed = !Equals(pricePlan.PaymentPlan, definition);
        if (!changed)
            return Results.Ok(ToResponse(pricePlan));

        pricePlan.DepositPercent = definition?.DepositPercent;
        pricePlan.InstalmentDayOfMonth = definition?.InstalmentDayOfMonth;
        pricePlan.FinalPaymentDueDaysBeforeDeparture = definition?.FinalPaymentDueDaysBeforeDeparture;
        pricePlan.Version++;
        pricePlan.UpdatedAtUtc = DateTimeOffset.UtcNow;

        pricing.Audits.Add(new PricingAuditRecord
        {
            Id = Guid.NewGuid(),
            PricePlanId = pricePlan.Id,
            DepartureId = departureId,
            ActorAccountId = authorization.Principal!.AccountId.Value,
            CorrelationId = http.TraceIdentifier,
            Action = definition is null ? "payment_plan_disabled" : "payment_plan_updated",
            Version = pricePlan.Version,
            Timestamp = pricePlan.UpdatedAtUtc
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
            "Payment plan outcome={Outcome} operatorId={OperatorId} departureId={DepartureId} pricingVersion={PricingVersion} correlationId={CorrelationId}",
            definition is null ? "disabled" : "updated",
            authorization.Access.OperatorId,
            departureId,
            pricePlan.Version,
            http.TraceIdentifier);

        return Results.Ok(ToResponse(pricePlan));
    }

    private static object ToResponse(PricePlanRecord plan) => new
    {
        pricingVersion = plan.Version,
        paymentPlan = plan.PaymentPlan is null ? null : new
        {
            enabled = true,
            depositPercent = plan.PaymentPlan.DepositPercent,
            instalmentDayOfMonth = plan.PaymentPlan.InstalmentDayOfMonth,
            finalPaymentDueDaysBeforeDeparture = plan.PaymentPlan.FinalPaymentDueDaysBeforeDeparture
        }
    };

    private static async Task<bool> OwnsDraftDepartureAsync(
        Guid departureId,
        string operatorId,
        CatalogueDbContext catalogue,
        CancellationToken cancellationToken) => await catalogue.DepartureBatches.AsNoTracking().AnyAsync(
            item =>
                item.Id == departureId &&
                item.OperatorId == operatorId &&
                item.Status == CatalogueDraftStatus.Draft,
            cancellationToken);

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

    private static IResult StalePricingVersion(HttpContext http) => Results.Problem(
        statusCode: 409,
        title: "Pricing changed",
        detail: "Pricing changed. Reload the latest configuration before saving the payment plan.",
        extensions: ProblemExtensions(http, "stale_pricing_version"));

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

public sealed record SavePaymentPlanRequest(
    int ExpectedPricingVersion,
    bool Enabled,
    decimal? DepositPercent,
    int? InstalmentDayOfMonth,
    int? FinalPaymentDueDaysBeforeDeparture);
