namespace NoorPath.Payments;

public sealed record PaymentCheckoutRequest(
    Guid PaymentAttemptId,
    Guid BookingId,
    string BookingReference,
    string AccountId,
    string Currency,
    decimal Amount,
    Uri ReturnUri,
    string ProviderIdempotencyKey,
    string CorrelationId);

public sealed record PaymentCheckoutSession(
    string Provider,
    string ProviderSessionId,
    Uri CheckoutUri,
    DateTimeOffset ExpiresAtUtc);

public sealed record PaymentProviderEvent(
    string Provider,
    string ProviderEventId,
    string ProviderSessionId,
    string EventType,
    PaymentAttemptState RequestedState,
    string PayloadHash,
    string SignatureKeyId,
    DateTimeOffset OccurredAtUtc);

public interface IPaymentProviderGateway
{
    string ProviderName { get; }

    Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken);
}

public interface IPaymentProviderEventVerifier
{
    string ProviderName { get; }

    ValueTask<PaymentProviderEvent?> VerifyAsync(
        ReadOnlyMemory<byte> payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}

public sealed class PaymentProviderUnavailableException(string message)
    : Exception(message);
