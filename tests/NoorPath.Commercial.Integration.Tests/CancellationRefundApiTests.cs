using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class CancellationRefundApiTests
{
    [Fact]
    public async Task Cancellation_is_account_scoped_idempotent_reviewed_and_fail_closed_for_refund_execution()
    {
        using var app = await BookingPaymentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        ConfigurePolicy(app);
        using var owner = app.CreateIdentityClient("cancellation-owner");
        using var other = app.CreateIdentityClient("cancellation-other");
        using var operatorClient = app.CreateIdentityClient(BookingPaymentApi.AuthorIdentity);

        var bookingId = await CreateConfirmedBookingAsync(
            app,
            owner,
            TestContext.Current.CancellationToken);

        var estimate = await owner.GetAsync(
            $"/api/v1/bookings/{bookingId:D}/cancellation",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, estimate.StatusCode);
        using var estimateBody = await ReadJsonAsync(
            estimate,
            TestContext.Current.CancellationToken);
        Assert.True(estimateBody.RootElement
            .GetProperty("policy")
            .GetProperty("canRequest")
            .GetBoolean());
        Assert.True(estimateBody.RootElement
            .GetProperty("policy")
            .GetProperty("requiresOperatorApproval")
            .GetBoolean());

        var hidden = await other.GetAsync(
            $"/api/v1/bookings/{bookingId:D}/cancellation",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var created = await RequestCancellationAsync(
            owner,
            bookingId,
            "cancel-request-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJsonAsync(
            created,
            TestContext.Current.CancellationToken);
        var cancellation = createdBody.RootElement.GetProperty("request");
        var cancellationId = cancellation.GetProperty("id").GetGuid();
        Assert.Equal("UnderReview", cancellation.GetProperty("customerStatus").GetString());
        Assert.Equal("vs16-test-policy-v1", cancellation.GetProperty("policyVersion").GetString());

        var replay = await RequestCancellationAsync(
            owner,
            bookingId,
            "cancel-request-0001",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var replayBody = await ReadJsonAsync(
            replay,
            TestContext.Current.CancellationToken);
        Assert.Equal(
            cancellationId,
            replayBody.RootElement.GetProperty("request").GetProperty("id").GetGuid());

        var queue = await operatorClient.GetAsync(
            "/api/v1/operator/cancellations?state=Requested",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        using var queueBody = await ReadJsonAsync(
            queue,
            TestContext.Current.CancellationToken);
        var item = Assert.Single(queueBody.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(cancellationId, item.GetProperty("cancellationId").GetGuid());
        var version = item.GetProperty("version").GetInt32();

        var approved = await operatorClient.PostAsJsonAsync(
            $"/api/v1/operator/cancellations/{cancellationId:D}/approve",
            new CancellationRefundEndpoints.OperatorDecisionBody(
                version,
                "Approved after reviewing the immutable policy calculation."),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var approvedBody = await ReadJsonAsync(
            approved,
            TestContext.Current.CancellationToken);
        Assert.Equal("RefundPending", approvedBody.RootElement.GetProperty("customerStatus").GetString());
        var refundId = approvedBody.RootElement.GetProperty("refundId").GetGuid();

        using (var scope = app.Services.CreateScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            var booking = await bookings.Bookings.SingleAsync(
                item => item.Id == bookingId,
                TestContext.Current.CancellationToken);
            Assert.Equal(BookingState.Cancelled, booking.State);
            Assert.Equal(cancellationId, booking.CancellationRequestId);
            Assert.NotNull(booking.CancelledAtUtc);

            var request = await bookings.CancellationRequests.SingleAsync(
                item => item.Id == cancellationId,
                TestContext.Current.CancellationToken);
            Assert.Equal(BookingCancellationState.Applied, request.State);
            Assert.NotNull(request.DecisionActorAccountId);
            Assert.NotNull(request.DecisionReason);
            Assert.True(request.RefundableAmount <= request.SettledAmount);

            Assert.Single(await inventory.Releases
                .Where(item => item.CancellationRequestId == cancellationId)
                .ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal(
                InventoryHoldState.Released,
                (await inventory.Holds.SingleAsync(
                    cancellationToken: TestContext.Current.CancellationToken)).State);

            var refund = await payments.Refunds.SingleAsync(
                item => item.Id == refundId,
                TestContext.Current.CancellationToken);
            Assert.Equal(RefundState.Authorized, refund.State);
            Assert.Equal(request.RefundableAmount, refund.Amount);
            Assert.Equal(0m, refund.RefundedAmount);
        }

        var detail = await operatorClient.GetAsync(
            $"/api/v1/operator/cancellations/{cancellationId:D}",
            TestContext.Current.CancellationToken);
        detail.EnsureSuccessStatusCode();
        using var detailBody = await ReadJsonAsync(
            detail,
            TestContext.Current.CancellationToken);
        var refundVersion = detailBody.RootElement
            .GetProperty("refund")
            .GetProperty("version")
            .GetInt32();

        var execution = await operatorClient.PostAsJsonAsync(
            $"/api/v1/operator/refunds/{refundId:D}/execute",
            new CancellationRefundEndpoints.OperatorDecisionBody(
                refundVersion,
                "Attempt approved refund through the configured provider gate."),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, execution.StatusCode);

        using (var scope = app.Services.CreateScope())
        {
            var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var refund = await payments.Refunds.SingleAsync(
                item => item.Id == refundId,
                TestContext.Current.CancellationToken);
            Assert.Equal(RefundState.Authorized, refund.State);
            Assert.Equal("refund_execution_disabled", refund.FailureCode);
            Assert.Equal(0m, refund.RefundedAmount);
        }
    }

    private static void ConfigurePolicy(BookingPaymentApi app)
    {
        using var scope = app.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<CancellationPolicyOptions>>()
            .Value;
        options.Enabled = true;
        options.Version = "vs16-test-policy-v1";
        options.TimeZoneId = "UTC";
        options.DepartureLocalTime = "10:00";
        options.RefundProcessingBusinessDays = 10;
        options.Windows =
        [
            new CancellationWindowOptions
            {
                MinimumDaysBeforeDeparture = 0,
                FeeBasisPoints = 1_000,
                NonRefundableAmount = 500m
            }
        ];
    }

    private static async Task<Guid> CreateConfirmedBookingAsync(
        BookingPaymentApi app,
        HttpClient customer,
        CancellationToken cancellationToken)
    {
        var departureId = await PublishDepartureAsync(app, cancellationToken);
        var travellerIds = new List<Guid>();
        for (var index = 0; index < 2; index++)
        {
            var response = await customer.PostAsJsonAsync(
                "/api/v1/travellers",
                new CreateTravellerRequest(
                    $"Cancellation Traveller {index + 1}",
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
        var holdId = holdBody.RootElement.GetProperty("holdId").GetGuid();

        using var bookingRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/inventory-holds/{holdId:D}/bookings");
        bookingRequest.Headers.Add("Idempotency-Key", $"booking-{Guid.NewGuid():N}");
        var booking = await customer.SendAsync(bookingRequest, cancellationToken);
        booking.EnsureSuccessStatusCode();
        using var bookingBody = await ReadJsonAsync(booking, cancellationToken);
        var bookingId = bookingBody.RootElement.GetProperty("bookingId").GetGuid();

        using var paymentRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/bookings/{bookingId:D}/payments");
        paymentRequest.Headers.Add("Idempotency-Key", $"payment-{Guid.NewGuid():N}");
        var payment = await customer.SendAsync(paymentRequest, cancellationToken);
        payment.EnsureSuccessStatusCode();
        using var paymentBody = await ReadJsonAsync(payment, cancellationToken);
        var orderId = paymentBody.RootElement
            .GetProperty("checkout")
            .GetProperty("providerSessionId")
            .GetString()!;

        using var webhookClient = app.CreateClient();
        using var webhook = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/payments/webhooks/razorpay");
        webhook.Headers.Add("x-razorpay-event-id", $"evt_cancel_{Guid.NewGuid():N}");
        webhook.Headers.Add("X-Test-Signature", "valid");
        webhook.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                eventType = "payment.captured",
                orderId,
                paymentId = $"pay_cancel_{Guid.NewGuid():N}",
                state = PaymentAttemptState.Succeeded.ToString(),
                occurredAtUtc = DateTimeOffset.UtcNow
            }),
            Encoding.UTF8,
            "application/json");
        var settled = await webhookClient.SendAsync(webhook, cancellationToken);
        settled.EnsureSuccessStatusCode();
        return bookingId;
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
                "VS-16 Cancellation Umrah",
                "Published departure for cancellation verification.",
                new(
                    "Makkah Cancellation Hotel",
                    "4 star",
                    "850 m from Masjid al-Haram",
                    7,
                    "confirmed"),
                new(
                    "Madinah Cancellation Hotel",
                    "4 star",
                    "450 m from Al-Masjid an-Nabawi",
                    5,
                    "confirmed"),
                new(
                    "Delhi → Jeddah → Makkah → Madinah",
                    "Published VS-16 routing.",
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
                "Initial VS-16 allocation",
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

    private static async Task<HttpResponseMessage> RequestCancellationAsync(
        HttpClient client,
        Guid bookingId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/bookings/{bookingId:D}/cancellation-requests");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(
            new CancellationRefundEndpoints.CancellationRequestBody("PlansChanged"));
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

    private sealed record PublicationReviewResponse(
        string Status,
        bool Ready,
        int DepartureVersion,
        int PricingVersion,
        int InventoryVersion);
}
