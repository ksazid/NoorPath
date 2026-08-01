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
                : (IResult)Results.Ok(new { accountId = principal.AccountId.Value });
        }).RequireAuthorization();

        endpoints.MapGet("/api/v1/platform/access", (HttpContext http, IConfiguration configuration) =>
        {
            var principal = http.User.GetCurrentPrincipal();
            if (principal is null) return Results.Unauthorized();

            var administrators = configuration
                .GetSection("Authorization:PlatformAdministratorAccountIds")
                .Get<string[]>() ?? [];

            return administrators.Contains(principal.AccountId.Value, StringComparer.Ordinal)
                ? Results.Ok(new { accountId = principal.AccountId.Value })
                : (IResult)Results.Forbid();
        }).RequireAuthorization();

        return endpoints;
    }
}
