namespace NoorPath.Architecture.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void ApiAssembly_is_available_to_architecture_tests()
    {
        Assert.NotNull(typeof(Program).Assembly);
    }
}

