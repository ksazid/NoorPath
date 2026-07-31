using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.Traveller.Infrastructure;

public sealed class TravellerDbContextFactory : IDesignTimeDbContextFactory<TravellerDbContext>
{
    public TravellerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("NOORPATH_MIGRATION_DB")
            ?? Environment.GetEnvironmentVariable("NOORPATH_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=noorpath_design;Username=noorpath;Password=noorpath";

        var options = new DbContextOptionsBuilder<TravellerDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(TravellerDbContext).Assembly.FullName))
            .Options;

        return new TravellerDbContext(options);
    }
}
