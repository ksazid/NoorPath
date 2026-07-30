using NoorPath.Pricing;
using Xunit;

namespace NoorPath.Pricing.Tests;

public sealed class PricingDraftTests
{
    [Fact]
    public void Normalizes_currency_and_orders_supported_occupancies()
    {
        var draft = new PricingDraft(new(
            " inr ",
            [
                new(PricingOccupancy.Quad, 90000m),
                new(PricingOccupancy.Double, 110000m),
                new(PricingOccupancy.Triple, 100000m)
            ]));

        Assert.Equal("INR", draft.Details.Currency);
        Assert.Equal(
            [PricingOccupancy.Double, PricingOccupancy.Triple, PricingOccupancy.Quad],
            draft.Details.Occupancies.Select(x => x.Occupancy));
    }

    [Theory]
    [InlineData("IN", 100)]
    [InlineData("1NR", 100)]
    [InlineData("INR", 0)]
    [InlineData("INR", -1)]
    [InlineData("INR", 10.001)]
    public void Rejects_invalid_currency_or_amount(string currency, double amount)
    {
        Assert.Throws<PricingDraftValidationException>(() => new PricingDraft(new(
            currency,
            [new(PricingOccupancy.Double, (decimal)amount)])));
    }

    [Fact]
    public void Rejects_duplicate_occupancy()
    {
        Assert.Throws<PricingDraftValidationException>(() => new PricingDraft(new(
            "INR",
            [
                new(PricingOccupancy.Double, 100m),
                new(PricingOccupancy.Double, 120m)
            ])));
    }
}
