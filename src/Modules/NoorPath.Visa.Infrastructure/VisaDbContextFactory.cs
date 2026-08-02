using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.Visa.Infrastructure;

public sealed class VisaDbContextFactory : IDesignTimeDbContextFactory<VisaDbContext>
{
    public VisaDbContext CreateDbContext(string[] args) { var options = new DbContextOptionsBuilder<VisaDbContext>().UseNpgsql("Host=localhost;Database=noorpath;Username=noorpath;Password=noorpath").Options; return new(options); }
}
