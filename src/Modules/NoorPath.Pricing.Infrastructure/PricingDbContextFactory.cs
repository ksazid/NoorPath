using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.Pricing.Infrastructure;

public sealed class PricingDbContextFactory : IDesignTimeDbContextFactory<PricingDbContext>
{
    public PricingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("NOORPATH_MIGRATION_DB")
            ?? Environment.GetEnvironmentVariable("NOORPATH_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=noorpath_design;Username=noorpath;Password=noorpath";

        var options = new DbContextOptionsBuilder<PricingDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(PricingDbContext).Assembly.FullName))
            .Options;

        return new PricingDbContext(options);
    }
}
