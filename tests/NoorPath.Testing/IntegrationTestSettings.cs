using Microsoft.AspNetCore.Hosting;

namespace NoorPath.Testing;

internal static class IntegrationTestSettings
{
    private const string LegacyDatabaseEnvironmentVariable = "NOORPATH_TEST_DB";

    public static string GetDatabaseConnection(
        string moduleEnvironmentVariable,
        string suiteName)
    {
        var connection = Environment.GetEnvironmentVariable(moduleEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connection))
            connection = Environment.GetEnvironmentVariable(LegacyDatabaseEnvironmentVariable);

        return !string.IsNullOrWhiteSpace(connection)
            ? connection
            : throw new InvalidOperationException(
                $"{moduleEnvironmentVariable} (or {LegacyDatabaseEnvironmentVariable}) is required for {suiteName} integration tests.");
    }

    public static void ConfigureTestHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("Authentication:Mode", "Test");
    }
}
