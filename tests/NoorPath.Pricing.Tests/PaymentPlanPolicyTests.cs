using NoorPath.Pricing;

namespace NoorPath.Pricing.Tests;

public sealed class PaymentPlanPolicyTests
{
    [Fact]
    public void Calculate_without_payment_plan_requires_full_amount_now()
    {
        var result = QuoteScheduleCalculator.Calculate(
            180_000m,
            new DateOnly(2027, 8, 14),
            new DateTimeOffset(2027, 1, 10, 10, 0, 0, TimeSpan.Zero),
            null);

        Assert.Equal(180_000m, result.Total);
        Assert.Equal(180_000m, result.DueNow);
        Assert.Equal(0m, result.Remaining);
        Assert.Empty(result.Instalments);
    }

    [Fact]
    public void Calculate_uses_published_plan_and_reconciles_exactly()
    {
        var result = QuoteScheduleCalculator.Calculate(
            180_000m,
            new DateOnly(2027, 8, 14),
            new DateTimeOffset(2027, 1, 10, 10, 0, 0, TimeSpan.Zero),
            new PaymentPlanDefinition(
                DepositPercent: 20m,
                InstalmentDayOfMonth: 5,
                FinalPaymentDueDaysBeforeDeparture: 30));

        Assert.Equal(180_000m, result.Total);
        Assert.Equal(36_000m, result.DueNow);
        Assert.Equal(144_000m, result.Remaining);
        Assert.NotEmpty(result.Instalments);
        Assert.Equal(new DateOnly(2027, 7, 15), result.Instalments[^1].DueDate);
        Assert.Equal(
            result.Total,
            result.DueNow + result.Instalments.Sum(item => item.Amount));
        Assert.Equal(result.Remaining, result.Instalments.Sum(item => item.Amount));
    }

    [Fact]
    public void Calculate_falls_back_to_full_amount_when_final_deadline_has_arrived()
    {
        var result = QuoteScheduleCalculator.Calculate(
            90_000m,
            new DateOnly(2027, 8, 14),
            new DateTimeOffset(2027, 7, 20, 8, 0, 0, TimeSpan.Zero),
            new PaymentPlanDefinition(20m, 5, 30));

        Assert.Equal(90_000m, result.DueNow);
        Assert.Equal(0m, result.Remaining);
        Assert.Empty(result.Instalments);
    }

    [Theory]
    [InlineData(0, 5, 30, "paymentPlan.depositPercent")]
    [InlineData(101, 5, 30, "paymentPlan.depositPercent")]
    [InlineData(20, 0, 30, "paymentPlan.instalmentDayOfMonth")]
    [InlineData(20, 29, 30, "paymentPlan.instalmentDayOfMonth")]
    [InlineData(20, 5, -1, "paymentPlan.finalPaymentDueDaysBeforeDeparture")]
    [InlineData(20, 5, 181, "paymentPlan.finalPaymentDueDaysBeforeDeparture")]
    public void Validate_rejects_invalid_payment_plan(
        decimal depositPercent,
        int instalmentDay,
        int finalDueDays,
        string field)
    {
        var errors = PaymentPlanPolicy.Validate(
            new PaymentPlanDefinition(depositPercent, instalmentDay, finalDueDays));

        Assert.Contains(field, errors.Keys);
    }

    [Fact]
    public void Calculate_final_instalment_absorbs_currency_rounding()
    {
        var result = QuoteScheduleCalculator.Calculate(
            100m,
            new DateOnly(2027, 5, 10),
            new DateTimeOffset(2027, 1, 2, 8, 0, 0, TimeSpan.Zero),
            new PaymentPlanDefinition(10m, 5, 20));

        Assert.Equal(100m, result.DueNow + result.Instalments.Sum(item => item.Amount));
        Assert.Equal(90m, result.Instalments.Sum(item => item.Amount));
    }
}
