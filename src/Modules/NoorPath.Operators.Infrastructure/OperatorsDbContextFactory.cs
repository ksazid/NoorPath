using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.Operators.Infrastructure;

public sealed class OperatorsDbContextFactory : IDesignTimeDbContextFactory<OperatorsDbContext>
{
    public OperatorsDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("NOORPATH_DB")
            ?? "Host=localhost;Port=5432;Database=noorpath;Username=noorpath;Password=local-development-only";
        return new(new DbContextOptionsBuilder<OperatorsDbContext>()
            .UseNpgsql(connection, postgres => postgres.MigrationsAssembly(typeof(OperatorsDbContext).Assembly.FullName))
            .Options);
    }
}
