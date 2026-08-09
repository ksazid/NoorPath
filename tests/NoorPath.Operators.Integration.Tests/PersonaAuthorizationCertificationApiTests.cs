using System.Net;
using Xunit;

namespace NoorPath.Operators.Integration.Tests;

public sealed class PersonaAuthorizationCertificationApiTests
{
    [Theory]
    [InlineData("customer-account", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData("approved-account", HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.Forbidden)]
    [InlineData("platform-administrator", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.OK)]
    public async Task Demo_identities_enforce_customer_operator_and_platform_boundaries(
        string accountId,
        HttpStatusCode accountStatus,
        HttpStatusCode operatorStatus,
        HttpStatusCode platformStatus)
    {
        using var app = await OperatorApi.CreateAsync();
        using var client = app.CreateClientFor(accountId);

        var account = await client.GetAsync(
            "/api/v1/account/access",
            TestContext.Current.CancellationToken);
        var operation = await client.GetAsync(
            "/api/v1/operator/access",
            TestContext.Current.CancellationToken);
        var platform = await client.GetAsync(
            "/api/v1/platform/access",
            TestContext.Current.CancellationToken);

        Assert.Equal(accountStatus, account.StatusCode);
        Assert.Equal(operatorStatus, operation.StatusCode);
        Assert.Equal(platformStatus, platform.StatusCode);
    }

    [Fact]
    public async Task Platform_operator_administration_is_not_available_without_authentication()
    {
        using var app = await OperatorApi.CreateAsync();
        using var client = app.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/platform/operators/summary",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
