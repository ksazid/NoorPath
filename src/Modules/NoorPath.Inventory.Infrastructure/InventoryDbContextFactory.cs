using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.Inventory.Infrastructure;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("NOORPATH_MIGRATION_DB")
            ?? Environment.GetEnvironmentVariable("NOORPATH_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=noorpath_design;Username=noorpath;Password=noorpath";

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName))
            .Options;

        return new InventoryDbContext(options);
    }
}
