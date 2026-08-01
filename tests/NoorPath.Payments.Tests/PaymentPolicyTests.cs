using NoorPath.Payments;
using Xunit;

namespace NoorPath.Payments.Tests;

public sealed class PaymentPolicyTests
{
    [Fact]
    public void Duplicate_provider_state_is_idempotent()
    {
        Assert.Equal(
            ProviderEventOutcome.Duplicate,
            PaymentPolicy.EvaluateProviderEvent(
                PaymentAttemptState.ProviderPending,
                PaymentAttemptState.ProviderPending));
    }

    [Fact]
    public void Settled_payment_does_not_regress()
    {
        Assert.Equal(
            ProviderEventOutcome.IgnoredOutOfOrder,
            PaymentPolicy.EvaluateProviderEvent(
                PaymentAttemptState.Succeeded,
                PaymentAttemptState.Failed));
    }

    [Fact]
    public void Pending_payment_can_settle()
    {
        Assert.Equal(
            ProviderEventOutcome.Applied,
            PaymentPolicy.EvaluateProviderEvent(
                PaymentAttemptState.ProviderPending,
                PaymentAttemptState.Succeeded));
    }

    [Theory]
    [InlineData("inr", 10)]
    [InlineData("IN", 10)]
    [InlineData("INR", 0)]
    [InlineData("INR", -1)]
    public void Invalid_money_is_rejected(string currency, decimal amount)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PaymentPolicy.ValidateAmount(currency, amount));
    }
}
