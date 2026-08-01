using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NoorPath.Payments;

public sealed class RazorpayOptions
{
    public const string SectionName = "Razorpay";

    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookSecretId { get; set; } = "primary";
    public Uri ApiBaseAddress { get; set; } = new("https://api.razorpay.com/");
    public Uri CheckoutScriptUri { get; set; } =
        new("https://checkout.razorpay.com/v1/checkout.js");
}

public sealed class RazorpayPaymentProvider(
    HttpClient client,
    IOptions<RazorpayOptions> options) :
    IPaymentProviderGateway,
    IPaymentCheckoutCallbackVerifier,
    IPaymentProviderEventVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RazorpayOptions _options = options.Value;

    public string ProviderName => "razorpay";

    public async Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        PaymentPolicy.ValidateAmount(request.Currency, request.Amount);
        if (!string.Equals(request.Currency, "INR", StringComparison.Ordinal))
        {
            throw new PaymentProviderUnavailableException(
                "The configured Razorpay checkout currently accepts INR only.");
        }

        var amountSubunits = ToSubunits(request.Amount);
        var receipt = $"{request.BookingReference}-{request.PaymentAttemptId:N}";
        if (receipt.Length > 40)
            receipt = receipt[..40];

        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/orders");
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{_options.KeyId}:{_options.KeySecret}")));
        message.Content = JsonContent.Create(new
        {
            amount = amountSubunits,
            currency = request.Currency,
            receipt,
            notes = new Dictionary<string, string>
            {
                ["booking_id"] = request.BookingId.ToString("D"),
                ["payment_attempt_id"] = request.PaymentAttemptId.ToString("D"),
                ["correlation_id"] = request.CorrelationId
            }
        }, options: JsonOptions);

        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new PaymentProviderUnavailableException(
                $"Razorpay order creation failed with HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var orderId = root.GetProperty("id").GetString();
        var status = root.GetProperty("status").GetString();
        var currency = root.GetProperty("currency").GetString();
        var returnedAmount = root.GetProperty("amount").GetInt64();

        if (string.IsNullOrWhiteSpace(orderId)
            || !string.Equals(status, "created", StringComparison.Ordinal)
            || !string.Equals(currency, request.Currency, StringComparison.Ordinal)
            || returnedAmount != amountSubunits)
        {
            throw new PaymentProviderUnavailableException(
                "Razorpay returned an invalid or mismatched order response.");
        }

        return new PaymentCheckoutSession(
            ProviderName,
            orderId,
            _options.KeyId,
            amountSubunits,
            request.Currency,
            _options.CheckoutScriptUri,
            request.ExpiresAtUtc);
    }

    public bool Verify(PaymentCheckoutCallback callback)
    {
        if (string.IsNullOrWhiteSpace(callback.ProviderSessionId)
            || string.IsNullOrWhiteSpace(callback.ProviderPaymentId)
            || string.IsNullOrWhiteSpace(callback.Signature))
        {
            return false;
        }

        var data = Encoding.UTF8.GetBytes(
            $"{callback.ProviderSessionId}|{callback.ProviderPaymentId}");
        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.KeySecret),
            data);
        return TryDecodeHex(callback.Signature, out var actual)
            && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public ValueTask<PaymentProviderEvent?> VerifyAsync(
        ReadOnlyMemory<byte> payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetHeader(headers, "X-Razorpay-Signature", out var signature)
            || !TryGetHeader(headers, "x-razorpay-event-id", out var providerEventId))
        {
            throw new CryptographicException("Required Razorpay webhook headers are missing.");
        }

        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.WebhookSecret),
            payload.Span);
        if (!TryDecodeHex(signature, out var actual)
            || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new CryptographicException("Razorpay webhook signature is invalid.");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = root.GetProperty("event").GetString();
        var requestedState = eventType switch
        {
            "payment.authorized" => PaymentAttemptState.ProviderPending,
            "payment.captured" => PaymentAttemptState.Succeeded,
            "order.paid" => PaymentAttemptState.Succeeded,
            "payment.failed" => PaymentAttemptState.Failed,
            _ => (PaymentAttemptState?)null
        };
        if (requestedState is null)
            return ValueTask.FromResult<PaymentProviderEvent?>(null);

        var providerSessionId = ReadOrderId(root);
        if (string.IsNullOrWhiteSpace(providerSessionId))
            throw new CryptographicException("Razorpay webhook order identity is missing.");

        var providerPaymentId = ReadPaymentId(root);
        var occurredAt = root.TryGetProperty("created_at", out var createdAt)
            ? DateTimeOffset.FromUnixTimeSeconds(createdAt.GetInt64())
            : DateTimeOffset.UtcNow;
        var payloadHash = Convert.ToHexString(SHA256.HashData(payload.Span));

        return ValueTask.FromResult<PaymentProviderEvent?>(new PaymentProviderEvent(
            ProviderName,
            providerEventId,
            providerSessionId,
            providerPaymentId,
            eventType!,
            requestedState.Value,
            payloadHash,
            _options.WebhookSecretId,
            occurredAt));
    }

    private static long ToSubunits(decimal amount)
    {
        var subunits = amount * 100m;
        if (subunits != decimal.Truncate(subunits)
            || subunits > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount cannot be represented in INR subunits.");
        }

        return decimal.ToInt64(subunits);
    }

    private static string? ReadOrderId(JsonElement root)
    {
        if (!root.TryGetProperty("payload", out var payload))
            return null;

        if (payload.TryGetProperty("payment", out var payment)
            && payment.TryGetProperty("entity", out var paymentEntity)
            && paymentEntity.TryGetProperty("order_id", out var paymentOrderId))
        {
            return paymentOrderId.GetString();
        }

        if (payload.TryGetProperty("order", out var order)
            && order.TryGetProperty("entity", out var orderEntity)
            && orderEntity.TryGetProperty("id", out var orderId))
        {
            return orderId.GetString();
        }

        return null;
    }

    private static string? ReadPaymentId(JsonElement root)
    {
        if (root.TryGetProperty("payload", out var payload)
            && payload.TryGetProperty("payment", out var payment)
            && payment.TryGetProperty("entity", out var paymentEntity)
            && paymentEntity.TryGetProperty("id", out var paymentId))
        {
            return paymentId.GetString();
        }

        return null;
    }

    private static bool TryGetHeader(
        IReadOnlyDictionary<string, string> headers,
        string name,
        out string value)
    {
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeHex(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
