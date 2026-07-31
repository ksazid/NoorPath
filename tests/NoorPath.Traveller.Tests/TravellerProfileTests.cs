using NoorPath.Traveller;

namespace NoorPath.Traveller.Tests;

public sealed class TravellerProfileTests
{
    [Fact]
    public void Constructor_normalizes_valid_name()
    {
        var profile = new TravellerProfile(
            new TravellerProfileDetails("  Amina   Khan  ", new DateOnly(1994, 5, 17)));

        Assert.Equal("Amina Khan", profile.Details.FullName);
        Assert.Equal(new DateOnly(1994, 5, 17), profile.Details.DateOfBirth);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Constructor_rejects_short_name(string fullName)
    {
        var exception = Assert.Throws<TravellerValidationException>(() =>
            new TravellerProfile(
                new TravellerProfileDetails(fullName, new DateOnly(1994, 5, 17))));

        Assert.Contains("fullName", exception.Errors.Keys);
    }

    [Fact]
    public void Constructor_rejects_today_or_future_date_of_birth()
    {
        var exception = Assert.Throws<TravellerValidationException>(() =>
            new TravellerProfile(
                new TravellerProfileDetails(
                    "Amina Khan",
                    DateOnly.FromDateTime(DateTime.UtcNow))));

        Assert.Contains("dateOfBirth", exception.Errors.Keys);
    }
}
