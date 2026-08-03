namespace NoorPath.Payments;

public enum RefundState
{
    NotRequired,
    Authorized,
    Processing,
    PartiallyRefunded,
    Refunded,
    Failed
}

public enum RefundProviderOutcome
{
    Succeeded,
    Pending,
    Failed,
    Unavailable
}

public sealed record RefundProviderRequest(
    Guid RefundId,
    Guid BookingId,
    Guid? PaymentAttemptId,
    string ProviderPaymentId,
    string Currency,
    decimal Amount,
    string IdempotencyKey,
    string CorrelationId);

public sealed record RefundProviderResult(
    RefundProviderOutcome Outcome,
    string? ProviderRefundId,
    decimal RefundedAmount,
    string? FailureCode)
{
    public static RefundProviderResult Succeeded(string providerRefundId, decimal amount) =>
        new(RefundProviderOutcome.Succeeded, providerRefundId, amount, null);

    public static RefundProviderResult Pending(string? providerRefundId = null) =>
        new(RefundProviderOutcome.Pending, providerRefundId, 0m, null);

    public static RefundProviderResult Failed(string failureCode) =>
        new(RefundProviderOutcome.Failed, null, 0m, failureCode);

    public static RefundProviderResult Unavailable(string failureCode) =>
        new(RefundProviderOutcome.Unavailable, null, 0m, failureCode);
}

public interface IRefundProviderGateway
{
    Task<RefundProviderResult> ExecuteAsync(
        RefundProviderRequest request,
        CancellationToken cancellationToken);
}

public static class RefundPolicy
{
    public static bool CanTransition(RefundState current, RefundState next) =>
        (current, next) switch
        {
            (RefundState.Authorized, RefundState.Processing) => true,
            (RefundState.Authorized, RefundState.Failed) => true,
            (RefundState.Processing, RefundState.PartiallyRefunded) => true,
            (RefundState.Processing, RefundState.Refunded) => true,
            (RefundState.Processing, RefundState.Failed) => true,
            (RefundState.Failed, RefundState.Processing) => true,
            _ => current == next
        };

    public static void Validate(string currency, decimal entitledAmount)
    {
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
            throw new ArgumentException("Currency must be a three-letter uppercase code.", nameof(currency));
        if (entitledAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(entitledAmount));
    }
}
