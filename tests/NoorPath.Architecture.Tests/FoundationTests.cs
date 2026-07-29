using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using Xunit;

namespace NoorPath.Architecture.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void ApiAssembly_is_available_to_architecture_tests() => Assert.NotNull(typeof(Program).Assembly);

    [Fact]
    public void Catalogue_domain_has_no_outward_framework_dependencies()
    {
        var references = typeof(Batch).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(references, x => x is not null && (x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) || x.StartsWith("Npgsql", StringComparison.Ordinal) || x.Contains("Infrastructure", StringComparison.Ordinal) || x.Contains("Api", StringComparison.Ordinal)));
    }

    [Fact]
    public void Catalogue_infrastructure_does_not_reference_operators_persistence()
    {
        var references = typeof(CatalogueDbContext).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(references, x => x is not null && x.Contains("Operators.Infrastructure", StringComparison.Ordinal));
    }
}
