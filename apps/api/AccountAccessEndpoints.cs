using System.Security.Claims;
using NoorPath.BuildingBlocks;

public static class AccountAccessEndpoints
{
    public static IEndpointRouteBuilder MapAccountAccess(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/account/access", (HttpContext http) =>
        {
            var principal = http.User.GetCurrentPrincipal();
            return principal is null
                ? Results.Unauthorized()
                : (IResult)Results.Ok(new
                {
                    accountId = principal.AccountId.Value,
                    displayName = SafeDisplayName(http.User)
                });
        }).RequireAuthorization();

        endpoints.MapGet("/api/v1/platform/access", (HttpContext http, IConfiguration configuration) =>
        {
            var principal = http.User.GetCurrentPrincipal();
            if (principal is null) return Results.Unauthorized();

            var administrators = configuration
                .GetSection("Authorization:PlatformAdministratorAccountIds")
                .Get<string[]>() ?? [];

            return administrators.Contains(principal.AccountId.Value, StringComparer.Ordinal)
                ? Results.Ok(new
                {
                    accountId = principal.AccountId.Value,
                    displayName = SafeDisplayName(http.User)
                })
                : (IResult)Results.Forbid();
        }).RequireAuthorization();

        return endpoints;
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
}
