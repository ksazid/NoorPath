using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable
namespace NoorPath.Catalogue.Infrastructure.Migrations;

[DbContext(typeof(CatalogueDbContext))]
public sealed class CatalogueDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => CatalogueDbContext.Configure(modelBuilder);
}
