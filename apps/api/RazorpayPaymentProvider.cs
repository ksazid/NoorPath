using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    IOptions<RazorpayOptions> options,
    TimeProvider timeProvider) :
    IPaymentProviderGateway,
    IPaymentCheckoutCallbackVerifier,
    IPaymentProviderEventVerifier
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);
    private readonly RazorpayOptions _options = options.Value;

    public string ProviderName => "razorpay";

    public async Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        PaymentPolicy.ValidateAmount(request.Currency, request.Amount);
        EnsureConfigured(requireWebhook: false);

        if (!string.Equals(request.Currency, "INR", StringComparison.Ordinal))
        {
            throw new PaymentProviderUnavailableException(
                "The configured Razorpay checkout currently accepts INR only.");
        }

        var amountSubunits = ToSubunits(request.Amount);
        var receipt = request.ProviderIdempotencyKey.Trim();
        if (receipt.Length is < 8 or > 40)
        {
            throw new ArgumentException(
                "The provider idempotency key must contain 8 to 40 characters.",
                nameof(request));
        }

        JsonElement? order = null;
        int? failureStatus = null;

        try
        {
            using var message = CreateAuthorizedRequest(HttpMethod.Post, "v1/orders");
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
            if (response.IsSuccessStatusCode)
            {
                order = await ReadRootAsync(response, cancellationToken);
            }
            else
            {
                failureStatus = (int)response.StatusCode;
            }
        }
        catch (HttpRequestException)
        {
            // A deterministic receipt lets a retry recover an order that Razorpay
            // accepted even when the original response was lost in transit.
        }

        order ??= await FindOrderByReceiptAsync(
            receipt,
            amountSubunits,
            request.Currency,
            cancellationToken);

        if (order is null)
        {
            throw new PaymentProviderUnavailableException(
                failureStatus is null
                    ? "Razorpay order creation did not return a recoverable order."
                    : $"Razorpay order creation failed with HTTP {failureStatus.Value}.");
        }

        var root = order.Value;
        var orderId = root.GetProperty("id").GetString();
        var status = root.GetProperty("status").GetString();
        var currency = root.GetProperty("currency").GetString();
        var returnedAmount = root.GetProperty("amount").GetInt64();
        var returnedReceipt = root.GetProperty("receipt").GetString();

        if (string.IsNullOrWhiteSpace(orderId)
            || status is not ("created" or "attempted" or "paid")
            || !string.Equals(currency, request.Currency, StringComparison.Ordinal)
            || returnedAmount != amountSubunits
            || !string.Equals(returnedReceipt, receipt, StringComparison.Ordinal))
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
        EnsureConfigured(requireWebhook: false);

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
        EnsureConfigured(requireWebhook: true);

        if (!TryGetHeader(headers, "X-Razorpay-Signature", out var signature)
            || !TryGetHeader(headers, "x-razorpay-event-id", out var providerEventId))
        {
            throw new CryptographicException(
                "Required Razorpay webhook headers are missing.");
        }

        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.WebhookSecret),
            payload.Span);
        if (!TryDecodeHex(signature, out var actual)
            || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new CryptographicException(
                "Razorpay webhook signature is invalid.");
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
        {
            throw new CryptographicException(
                "Razorpay webhook order identity is missing.");
        }

        var providerPaymentId = ReadPaymentId(root);
        var occurredAt = root.TryGetProperty("created_at", out var createdAt)
            ? DateTimeOffset.FromUnixTimeSeconds(createdAt.GetInt64())
            : timeProvider.GetUtcNow();
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

    private async Task<JsonElement?> FindOrderByReceiptAsync(
        string receipt,
        long amountSubunits,
        string currency,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"v1/orders?receipt={Uri.EscapeDataString(receipt)}&count=2");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement? exact = null;
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("receipt", out var returnedReceipt)
                || !string.Equals(
                    returnedReceipt.GetString(),
                    receipt,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (exact is not null)
            {
                throw new PaymentProviderUnavailableException(
                    "Razorpay returned multiple orders for one unique receipt.");
            }

            exact = item.Clone();
        }

        if (exact is null)
            return null;

        var root = exact.Value;
        if (root.GetProperty("amount").GetInt64() != amountSubunits
            || !string.Equals(
                root.GetProperty("currency").GetString(),
                currency,
                StringComparison.Ordinal))
        {
            throw new PaymentProviderUnavailableException(
                "The recovered Razorpay order does not match the booking obligation.");
        }

        return exact;
    }

    private HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string uri)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{_options.KeyId}:{_options.KeySecret}")));
        return message;
    }

    private static async Task<JsonElement> ReadRootAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private void EnsureConfigured(bool requireWebhook)
    {
        if (string.IsNullOrWhiteSpace(_options.KeyId)
            || string.IsNullOrWhiteSpace(_options.KeySecret)
            || (requireWebhook && string.IsNullOrWhiteSpace(_options.WebhookSecret)))
        {
            throw new PaymentProviderUnavailableException(
                "Razorpay credentials are not configured for this environment.");
        }
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
