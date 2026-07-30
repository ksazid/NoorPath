using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NoorPath.BuildingBlocks;

public sealed record CurrentPrincipal(AccountId AccountId);

public static class NoorPathAuthentication
{
    public const string TestScheme = "NoorPathTest";

    public static IServiceCollection AddNoorPathAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mode = configuration["Authentication:Mode"] ?? (environment.IsDevelopment() ? "Test" : "Bearer");
        if (mode.Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
                throw new InvalidOperationException("Test authentication is allowed only in Development or Test environments.");

            services.AddAuthentication(TestScheme).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestScheme, null);
        }
        else if (mode.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            var authority = configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Authentication:Authority is required for bearer authentication.");
            var audience = configuration["Authentication:Audience"] ?? throw new InvalidOperationException("Authentication:Audience is required for bearer authentication.");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = "sub" };
            });
        }
        else throw new InvalidOperationException("Authentication:Mode must be Test or Bearer.");

        services.AddAuthorization();
        return services;
    }

    public static CurrentPrincipal? GetCurrentPrincipal(this ClaimsPrincipal principal)
    {
        var accountId = principal.FindFirstValue("noorpath_account_id");
        if (!string.IsNullOrWhiteSpace(accountId)) return accountId.Length <= 120 ? new(new(accountId)) : null;

        var issuer = principal.FindFirstValue("iss");
        var subject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)) return null;

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{issuer}\n{subject}"));
        return new(new($"oidc:{Convert.ToHexString(digest).ToLowerInvariant()}"));
    }

    public static IApplicationBuilder UseNoorPathAuthenticationErrors(
    this IApplicationBuilder app)
{
    return app.UseStatusCodePages(
        async (Microsoft.AspNetCore.Diagnostics.StatusCodeContext context) =>
        {
            var httpContext = context.HttpContext;
            var response = httpContext.Response;

            if (response.StatusCode is not (401 or 403))
                return;

            response.ContentType = "application/problem+json";

            await response.WriteAsJsonAsync(
                new
                {
                    type = "about:blank",
                    title = response.StatusCode == 401
                        ? "Sign in required"
                        : "Access unavailable",
                    status = response.StatusCode,
                    code = response.StatusCode == 401
                        ? "not_authenticated"
                        : "forbidden",
                    correlationId = httpContext.TraceIdentifier
                },
                cancellationToken: httpContext.RequestAborted);
        });
}
}

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-NoorPath-Test-Identity", out var value) || string.IsNullOrWhiteSpace(value) || value.ToString().Length > 120)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[] { new Claim("noorpath_account_id", value.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new(principal, Scheme.Name)));
    }
}
