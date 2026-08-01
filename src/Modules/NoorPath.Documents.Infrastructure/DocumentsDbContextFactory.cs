using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace NoorPath.Documents.Infrastructure;

public sealed class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__NoorPath") ?? "Host=localhost;Database=noorpath;Username=noorpath;Password=noorpath";
        return new(new DbContextOptionsBuilder<DocumentsDbContext>().UseNpgsql(connection).Options);
    }
}
