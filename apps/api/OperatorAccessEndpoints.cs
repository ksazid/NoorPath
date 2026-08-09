using System.Security.Claims;
using NoorPath.Operators;

public static class OperatorAccessEndpoints
{
    public static void MapOperatorAccess(this WebApplication app)
    {
        app.MapGet("/api/v1/operator/access", async (
            HttpContext http,
            IOperatorAccess operators,
            ILogger<Program> log,
            CancellationToken cancellationToken) =>
        {
            var principal = http.User.GetCurrentPrincipal();
            if (principal is null)
                return Results.Problem(statusCode: 401, title: "Sign in required", extensions: ProblemExtensions(http, "not_authenticated"));

            var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
            if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            {
                log.LogWarning("Operator access outcome={Outcome} accountId={AccountId} operatorId={OperatorId} correlationId={CorrelationId}", "denied", principal.AccountId, access?.OperatorId, http.TraceIdentifier);
                return Results.Problem(statusCode: 403, title: "Access unavailable", detail: "This account does not have access to operator administration.", extensions: ProblemExtensions(http, "forbidden"));
            }

            log.LogInformation("Operator access outcome={Outcome} accountId={AccountId} operatorId={OperatorId} correlationId={CorrelationId}", "allowed", principal.AccountId, access.OperatorId, http.TraceIdentifier);
            return Results.Ok(new
            {
                accountId = principal.AccountId.Value,
                displayName = SafeDisplayName(http.User),
                @operator = new { id = access.OperatorId, displayName = access.OperatorDisplayName },
                permissions = access.Permissions.Order()
            });
        }).RequireAuthorization();
    }

    private static string SafeDisplayName(ClaimsPrincipal user)
    {
        var displayName = user.FindFirstValue("name")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("nickname")
            ?? user.FindFirstValue("given_name");

        if (string.IsNullOrWhiteSpace(displayName)) return "NoorPath member";
        displayName = displayName.Trim();
        return displayName.Length <= 80 ? displayName : displayName[..80];
    }

    private static Dictionary<string, object?> ProblemExtensions(HttpContext http, string code) => new()
    {
        ["code"] = code,
        ["correlationId"] = http.TraceIdentifier
    };
}
