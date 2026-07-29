using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.Catalogue.Infrastructure;

public sealed class CatalogueDbContextFactory : IDesignTimeDbContextFactory<CatalogueDbContext>
{
    public CatalogueDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("NOORPATH_MIGRATION_DB")
            ?? Environment.GetEnvironmentVariable("NOORPATH_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=noorpath_design;Username=noorpath;Password=noorpath";

        var options = new DbContextOptionsBuilder<CatalogueDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(CatalogueDbContext).Assembly.FullName))
            .Options;

        return new CatalogueDbContext(options);
    }
}