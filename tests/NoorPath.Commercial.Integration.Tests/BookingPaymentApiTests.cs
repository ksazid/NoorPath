using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class BookingPaymentApiTests
{
    [Fact]
    public async Task Booking_is_idempotent_account_scoped_and_snapshots_reviewed_truth()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var customer = app.CreateIdentityClient("booking-owner");
        using var other = app.CreateIdentityClient("booking-other");
        var journey = await CreateJourneyAsync(
            app,
            customer,
            "Booking Owner",
            TestContext.Current.CancellationToken);

        var created = await SendBookingAsync(
            customer,
            journey.HoldId,
            "booking-create-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJsonAsync(
            created,
            TestContext.Current.CancellationToken);
        var bookingId = createdBody.RootElement.GetProperty("bookingId").GetGuid();
        var bookingReference = createdBody.RootElement
            .GetProperty("bookingReference")
            .GetString();
        Assert.StartsWith("NP-", bookingReference, StringComparison.Ordinal);
        Assert.Equal("PendingPayment", createdBody.RootElement.GetProperty("state").GetString());
        Assert.Equal(journey.QuoteId, createdBody.RootElement.GetProperty("quoteId").GetGuid());
        Assert.Equal(journey.HoldId, createdBody.RootElement.GetProperty("inventoryHoldId").GetGuid());
        Assert.Equal("INR", createdBody.RootElement.GetProperty("currency").GetString());
        Assert.Equal(
            createdBody.RootElement.GetProperty("total").GetDecimal(),
            createdBody.RootElement.GetProperty("dueNow").GetDecimal()
            + createdBody.RootElement.GetProperty("remaining").GetDecimal());

        var travellers = createdBody.RootElement.GetProperty("travellers")
            .EnumerateArray()
            .ToArray();
        Assert.Equal(2, travellers.Length);
        Assert.Equal("Booking Owner One", travellers[0].GetProperty("fullName").GetString());
        Assert.Equal("Booking Owner Two", travellers[1].GetProperty("fullName").GetString());
        Assert.Equal(
            new DateOnly(1990, 1, 2),
            travellers[0].GetProperty("dateOfBirth").GetDateTime().ToDateOnly());

        var replay = await SendBookingAsync(
            customer,
            journey.HoldId,
            "booking-create-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var replayBody = await ReadJsonAsync(
            replay,
            TestContext.Current.CancellationToken);
        Assert.Equal(bookingId, replayBody.RootElement.GetProperty("bookingId").GetGuid());

        var secondKey = await SendBookingAsync(
            customer,
            journey.HoldId,
            "booking-create-0002",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, secondKey.StatusCode);
        Assert.Equal(
            "booking_exists_for_hold",
            await ReadProblemCodeAsync(secondKey, TestContext.Current.CancellationToken));

        var hidden = await other.GetAsync(
            $"/api/v1/bookings/{bookingId:D}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        using var scope = app.Services.CreateScope();
        var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Single(await bookings.Bookings.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            2,
            await bookings.Travellers.CountAsync(TestContext.Current.CancellationToken));
        Assert.Single(await bookings.OutboxMessages
            .Where(item => item.EventType == "BookingCreated")
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Concurrent_booking_requests_for_one_hold_create_exactly_one_booking()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var customer = app.CreateIdentityClient("booking-race-owner");
        var journey = await CreateJourneyAsync(
            app,
            customer,
            "Booking Race",
            TestContext.Current.CancellationToken);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(index =>
            SendBookingAsync(
                customer,
                journey.HoldId,
                $"booking-race-{index:D4}",
                TestContext.Current.CancellationToken)));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        using var scope = app.Services.CreateScope();
        var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Equal(
            1,
            await bookings.Bookings.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            2,
            await bookings.Travellers.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Payment_initiation_is_idempotent_and_keeps_one_provider_order()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var customer = app.CreateIdentityClient("payment-owner");
        var bookingId = await CreateBookingAsync(
            app,
            customer,
            "Payment Owner",
            TestContext.Current.CancellationToken);

        var created = await SendPaymentAsync(
            customer,
            bookingId,
            "payment-create-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJsonAsync(
            created,
            TestContext.Current.CancellationToken);
        var paymentAttemptId = createdBody.RootElement
            .GetProperty("paymentAttemptId")
            .GetGuid();
        var checkout = createdBody.RootElement.GetProperty("checkout");
        var orderId = checkout.GetProperty("providerSessionId").GetString();
        Assert.Equal($"order_{paymentAttemptId:N}", orderId);
        Assert.Equal("ProviderPending", createdBody.RootElement.GetProperty("state").GetString());

        var replay = await SendPaymentAsync(
            customer,
            bookingId,
            "payment-create-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var replayBody = await ReadJsonAsync(
            replay,
            TestContext.Current.CancellationToken);
        Assert.Equal(
            paymentAttemptId,
            replayBody.RootElement.GetProperty("paymentAttemptId").GetGuid());
        Assert.Equal(
            orderId,
            replayBody.RootElement
                .GetProperty("checkout")
                .GetProperty("providerSessionId")
                .GetString());

        var secondKey = await SendPaymentAsync(
            customer,
            bookingId,
            "payment-create-0002",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, secondKey.StatusCode);
        Assert.Equal(
            "payment_attempt_exists",
            await ReadProblemCodeAsync(secondKey, TestContext.Current.CancellationToken));

        using var scope = app.Services.CreateScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Single(await payments.PaymentAttempts.ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Equal(
            BookingState.PaymentInProgress,
            (await bookings.Bookings.SingleAsync(
                cancellationToken: TestContext.Current.CancellationToken)).State);
    }

    [Fact]
    public async Task Callback_only_verifies_identity_and_webhook_is_authoritative_for_settlement()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var customer = app.CreateIdentityClient("settlement-owner");
        var bookingId = await CreateBookingAsync(
            app,
            customer,
            "Settlement Owner",
            TestContext.Current.CancellationToken);
        var payment = await SendPaymentAsync(
            customer,
            bookingId,
            "settlement-payment-0001",
            TestContext.Current.CancellationToken);
        payment.EnsureSuccessStatusCode();
        using var paymentBody = await ReadJsonAsync(
            payment,
            TestContext.Current.CancellationToken);
        var paymentAttemptId = paymentBody.RootElement
            .GetProperty("paymentAttemptId")
            .GetGuid();
        var orderId = paymentBody.RootElement
            .GetProperty("checkout")
            .GetProperty("providerSessionId")
            .GetString()!;
        const string providerPaymentId = "pay_test_settled_0001";

        var callback = await customer.PostAsJsonAsync(
            $"/api/v1/payments/{paymentAttemptId:D}/checkout-callback",
            new PaymentEndpoints.CheckoutCallbackRequest(
                orderId,
                providerPaymentId,
                TestPaymentProvider.SignatureFor(orderId, providerPaymentId)),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, callback.StatusCode);
        using var callbackBody = await ReadJsonAsync(
            callback,
            TestContext.Current.CancellationToken);
        Assert.False(callbackBody.RootElement.GetProperty("settlementVerified").GetBoolean());
        Assert.Equal("ProviderPending", callbackBody.RootElement.GetProperty("state").GetString());

        var settled = await SendWebhookAsync(
            app,
            eventId: "evt_settled_0001",
            orderId,
            providerPaymentId,
            PaymentAttemptState.Succeeded,
            "payment.captured",
            validSignature: true,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, settled.StatusCode);
        using var settledBody = await ReadJsonAsync(
            settled,
            TestContext.Current.CancellationToken);
        Assert.Equal("Applied", settledBody.RootElement.GetProperty("outcome").GetString());
        Assert.Equal("Succeeded", settledBody.RootElement.GetProperty("state").GetString());

        var duplicate = await SendWebhookAsync(
            app,
            eventId: "evt_settled_0001",
            orderId,
            providerPaymentId,
            PaymentAttemptState.Succeeded,
            "payment.captured",
            validSignature: true,
            TestContext.Current.CancellationToken);
        duplicate.EnsureSuccessStatusCode();
        using var duplicateBody = await ReadJsonAsync(
            duplicate,
            TestContext.Current.CancellationToken);
        Assert.True(duplicateBody.RootElement.GetProperty("duplicate").GetBoolean());

        var lateFailure = await SendWebhookAsync(
            app,
            eventId: "evt_failed_late_0001",
            orderId,
            providerPaymentId,
            PaymentAttemptState.Failed,
            "payment.failed",
            validSignature: true,
            TestContext.Current.CancellationToken);
        lateFailure.EnsureSuccessStatusCode();
        using var lateFailureBody = await ReadJsonAsync(
            lateFailure,
            TestContext.Current.CancellationToken);
        Assert.Equal(
            "IgnoredOutOfOrder",
            lateFailureBody.RootElement.GetProperty("outcome").GetString());
        Assert.Equal("Succeeded", lateFailureBody.RootElement.GetProperty("state").GetString());

        using var scope = app.Services.CreateScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var attempt = await payments.PaymentAttempts.SingleAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(PaymentAttemptState.Succeeded, attempt.State);
        Assert.Equal(providerPaymentId, attempt.ProviderPaymentId);
        Assert.NotNull(attempt.SettledAtUtc);
        Assert.Equal(
            2,
            await payments.ProviderEvents.CountAsync(
                TestContext.Current.CancellationToken));
        Assert.Equal(
            BookingState.PaymentSucceeded,
            (await bookings.Bookings.SingleAsync(
                cancellationToken: TestContext.Current.CancellationToken)).State);
    }

    [Fact]
    public async Task Invalid_provider_signatures_do_not_change_payment_state()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var customer = app.CreateIdentityClient("invalid-signature-owner");
        var bookingId = await CreateBookingAsync(
            app,
            customer,
            "Invalid Signature",
            TestContext.Current.CancellationToken);
        var payment = await SendPaymentAsync(
            customer,
            bookingId,
            "invalid-signature-payment-0001",
            TestContext.Current.CancellationToken);
        payment.EnsureSuccessStatusCode();
        using var paymentBody = await ReadJsonAsync(
            payment,
            TestContext.Current.CancellationToken);
        var paymentAttemptId = paymentBody.RootElement
            .GetProperty("paymentAttemptId")
            .GetGuid();
        var orderId = paymentBody.RootElement
            .GetProperty("checkout")
            .GetProperty("providerSessionId")
            .GetString()!;

        var callback = await customer.PostAsJsonAsync(
            $"/api/v1/payments/{paymentAttemptId:D}/checkout-callback",
            new PaymentEndpoints.CheckoutCallbackRequest(
                orderId,
                "pay_invalid",
                "invalid"),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, callback.StatusCode);

        var webhook = await SendWebhookAsync(
            app,
            eventId: "evt_invalid_0001",
            orderId,
            "pay_invalid",
            PaymentAttemptState.Succeeded,
            "payment.captured",
            validSignature: false,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, webhook.StatusCode);

        using var scope = app.Services.CreateScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var attempt = await payments.PaymentAttempts.SingleAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(PaymentAttemptState.ProviderPending, attempt.State);
        Assert.Null(attempt.ProviderPaymentId);
        Assert.Empty(await payments.ProviderEvents.ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Payment_reads_and_callbacks_are_hidden_from_other_accounts()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var owner = app.CreateIdentityClient("payment-scope-owner");
        using var other = app.CreateIdentityClient("payment-scope-other");
        var bookingId = await CreateBookingAsync(
            app,
            owner,
            "Payment Scope",
            TestContext.Current.CancellationToken);
        var payment = await SendPaymentAsync(
            owner,
            bookingId,
            "payment-scope-0001",
            TestContext.Current.CancellationToken);
        payment.EnsureSuccessStatusCode();
        using var body = await ReadJsonAsync(payment, TestContext.Current.CancellationToken);
        var paymentAttemptId = body.RootElement.GetProperty("paymentAttemptId").GetGuid();
        var orderId = body.RootElement
            .GetProperty("checkout")
            .GetProperty("providerSessionId")
            .GetString()!;

        var hidden = await other.GetAsync(
            $"/api/v1/payments/{paymentAttemptId:D}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var hiddenCallback = await other.PostAsJsonAsync(
            $"/api/v1/payments/{paymentAttemptId:D}/checkout-callback",
            new PaymentEndpoints.CheckoutCallbackRequest(
                orderId,
                "pay_hidden",
                TestPaymentProvider.SignatureFor(orderId, "pay_hidden")),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hiddenCallback.StatusCode);
    }

    private static async Task<Guid> CreateBookingAsync(
        BookingPaymentApi app,
        HttpClient customer,
        string travellerPrefix,
        CancellationToken cancellationToken)
    {
        var journey = await CreateJourneyAsync(
            app,
            customer,
            travellerPrefix,
            cancellationToken);
        var booking = await SendBookingAsync(
            customer,
            journey.HoldId,
            $"booking-{Guid.NewGuid():N}",
            cancellationToken);
        booking.EnsureSuccessStatusCode();
        using var body = await ReadJsonAsync(booking, cancellationToken);
        return body.RootElement.GetProperty("bookingId").GetGuid();
    }

    private static async Task<JourneyState> CreateJourneyAsync(
        BookingPaymentApi app,
        HttpClient customer,
        string travellerPrefix,
        CancellationToken cancellationToken)
    {
        var departureId = await PublishDepartureAsync(app, cancellationToken);
        var travellerIds = new List<Guid>();
        for (var index = 0; index < 2; index++)
        {
            var response = await customer.PostAsJsonAsync(
                "/api/v1/travellers",
                new CreateTravellerRequest(
                    $"{travellerPrefix} {(index == 0 ? "One" : "Two")}",
                    new DateOnly(1990 + index, 1, 2 + index).ToString("yyyy-MM-dd")),
                cancellationToken);
            response.EnsureSuccessStatusCode();
            using var body = await ReadJsonAsync(response, cancellationToken);
            travellerIds.Add(body.RootElement.GetProperty("travellerId").GetGuid());
        }

        var quote = await customer.PostAsJsonAsync(
            $"/api/v1/departures/{departureId:D}/quotes",
            new CreateQuoteRequest("double", travellerIds),
            cancellationToken);
        quote.EnsureSuccessStatusCode();
        using var quoteBody = await ReadJsonAsync(quote, cancellationToken);
        var quoteId = quoteBody.RootElement.GetProperty("quoteId").GetGuid();

        using var holdRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/quotes/{quoteId:D}/holds");
        holdRequest.Headers.Add("Idempotency-Key", $"hold-{Guid.NewGuid():N}");
        var hold = await customer.SendAsync(holdRequest, cancellationToken);
        hold.EnsureSuccessStatusCode();
        using var holdBody = await ReadJsonAsync(hold, cancellationToken);
        return new JourneyState(
            departureId,
            quoteId,
            holdBody.RootElement.GetProperty("holdId").GetGuid(),
            travellerIds);
    }

    private static async Task<Guid> PublishDepartureAsync(
        BookingPaymentApi app,
        CancellationToken cancellationToken)
    {
        using var author = app.CreateIdentityClient(BookingPaymentApi.AuthorIdentity);
        using var approver = app.CreateIdentityClient(BookingPaymentApi.ApproverIdentity);
        var departureDate = new DateOnly(2027, 10, 10);
        var draft = await author.PostAsJsonAsync(
            "/api/v1/operator/departures",
            new SaveCatalogueDraftRequest(
                "VS-09 Booking & Payment Umrah",
                "Published departure for booking and payment verification.",
                new(
                    "Makkah Booking Hotel",
                    "4 star",
                    "850 m from Masjid al-Haram",
                    7,
                    "confirmed"),
                new(
                    "Madinah Booking Hotel",
                    "4 star",
                    "450 m from Al-Masjid an-Nabawi",
                    5,
                    "confirmed"),
                new(
                    "Delhi → Jeddah → Makkah → Madinah",
                    "Published VS-09 routing.",
                    "confirmed"),
                "Delhi (DEL)",
                departureDate,
                departureDate.AddDays(12),
                ["Return flights", "Breakfast", "Journey support"],
                ["Personal expenses"]),
            cancellationToken);
        draft.EnsureSuccessStatusCode();
        using var draftBody = await ReadJsonAsync(draft, cancellationToken);
        var departureId = draftBody.RootElement.GetProperty("departureId").GetGuid();

        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId:D}/pricing",
            new SavePricingRequest(
                0,
                "INR",
                [
                    new("double", 110000m),
                    new("triple", 100000m),
                    new("quad", 90000m)
                ]),
            cancellationToken)).EnsureSuccessStatusCode();
        (await author.PutAsJsonAsync(
            $"/api/v1/operator/departures/{departureId:D}/inventory",
            new SaveInventoryRequest(
                0,
                "Initial VS-09 allocation",
                [
                    new("double", 12),
                    new("triple", 8),
                    new("quad", 6)
                ]),
            cancellationToken)).EnsureSuccessStatusCode();

        var review = await author.GetFromJsonAsync<PublicationReviewResponse>(
            $"/api/v1/operator/departures/{departureId:D}/publication-review",
            cancellationToken);
        Assert.NotNull(review);
        (await author.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId:D}/submit-review",
            new PublicationVersionRequest(
                review.DepartureVersion,
                review.PricingVersion,
                review.InventoryVersion),
            cancellationToken)).EnsureSuccessStatusCode();

        var submitted = await approver.GetFromJsonAsync<PublicationReviewResponse>(
            $"/api/v1/platform/publications/{departureId:D}",
            cancellationToken);
        Assert.NotNull(submitted);
        (await approver.PostAsJsonAsync(
            $"/api/v1/platform/publications/{departureId:D}/publish",
            new PublicationVersionRequest(
                submitted.DepartureVersion,
                submitted.PricingVersion,
                submitted.InventoryVersion),
            cancellationToken)).EnsureSuccessStatusCode();
        return departureId;
    }

    private static async Task<HttpResponseMessage> SendBookingAsync(
        HttpClient customer,
        Guid holdId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/inventory-holds/{holdId:D}/bookings");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await customer.SendAsync(request, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendPaymentAsync(
        HttpClient customer,
        Guid bookingId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/bookings/{bookingId:D}/payments");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await customer.SendAsync(request, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendWebhookAsync(
        BookingPaymentApi app,
        string eventId,
        string orderId,
        string? paymentId,
        PaymentAttemptState state,
        string eventType,
        bool validSignature,
        CancellationToken cancellationToken)
    {
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/payments/webhooks/razorpay");
        request.Headers.Add("x-razorpay-event-id", eventId);
        request.Headers.Add(
            "X-Test-Signature",
            validSignature ? "valid" : "invalid");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                eventType,
                orderId,
                paymentId,
                state = state.ToString(),
                occurredAtUtc = DateTimeOffset.UtcNow
            }),
            Encoding.UTF8,
            "application/json");
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<string?> ReadProblemCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var body = await ReadJsonAsync(response, cancellationToken);
        return body.RootElement.GetProperty("code").GetString();
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

    private sealed record JourneyState(
        Guid DepartureId,
        Guid QuoteId,
        Guid HoldId,
        IReadOnlyList<Guid> TravellerIds);

    private sealed record PublicationReviewResponse(
        string Status,
        bool Ready,
        int DepartureVersion,
        int PricingVersion,
        int InventoryVersion);
}

internal static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime value) => DateOnly.FromDateTime(value);
}
