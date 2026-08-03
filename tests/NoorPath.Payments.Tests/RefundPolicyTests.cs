using NoorPath.Payments;
using Xunit;

namespace NoorPath.Payments.Tests;

public sealed class RefundPolicyTests
{
    [Theory]
    [InlineData(RefundState.Authorized, RefundState.Processing)]
    [InlineData(RefundState.Processing, RefundState.PartiallyRefunded)]
    [InlineData(RefundState.Processing, RefundState.Refunded)]
    [InlineData(RefundState.Processing, RefundState.Failed)]
    [InlineData(RefundState.Failed, RefundState.Processing)]
    public void Approved_refund_transitions_are_explicit(
        RefundState current,
        RefundState next) =>
        Assert.True(RefundPolicy.CanTransition(current, next));

    [Fact]
    public void Settled_refund_cannot_return_to_processing() =>
        Assert.False(RefundPolicy.CanTransition(RefundState.Refunded, RefundState.Processing));

    [Fact]
    public void Validate_rejects_negative_entitlement() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RefundPolicy.Validate("INR", -1m));

    [Fact]
    public void Validate_rejects_invalid_currency() =>
        Assert.Throws<ArgumentException>(() =>
            RefundPolicy.Validate("inr", 100m));
}
