using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Testing;
using NoorPath.Traveller.Infrastructure;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class BookingPaymentApi : WebApplicationFactory<Program>
{
    public const string AuthorIdentity = "vs09-author";
    public const string ApproverIdentity = "vs09-approver";

    private readonly string connection;

    private BookingPaymentApi(string connection) => this.connection = connection;

    public static async Task<BookingPaymentApi> CreateAsync(
        CancellationToken cancellationToken)
    {
        var connection = IntegrationTestSettings.GetDatabaseConnection(
            "NOORPATH_COMMERCIAL_TEST_DB",
            "VS-09 booking and payment API");
        var app = new BookingPaymentApi(connection);

        using var scope = app.Services.CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var catalogue = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        var operators = scope.ServiceProvider.GetRequiredService<OperatorsDbContext>();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var pricing = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var traveller = scope.ServiceProvider.GetRequiredService<TravellerDbContext>();

        await catalogue.Database.EnsureDeletedAsync(cancellationToken);
        await operators.Database.MigrateAsync(cancellationToken);
        await catalogue.Database.MigrateAsync(cancellationToken);
        await pricing.Database.MigrateAsync(cancellationToken);
        await inventory.Database.MigrateAsync(cancellationToken);
        await traveller.Database.MigrateAsync(cancellationToken);
        await booking.Database.MigrateAsync(cancellationToken);
        await payments.Database.MigrateAsync(cancellationToken);
        await SeedOperatorsAsync(operators, cancellationToken);
        return app;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestSettings.ConfigureTestHost(builder);
        builder.UseSetting(
            "Authorization:PlatformPublicationApproverAccountIds:0",
            ApproverIdentity);

        builder.ConfigureServices(services =>
        {
            RemoveContext<BookingDbContext>(services);
            RemoveContext<CatalogueDbContext>(services);
            RemoveContext<OperatorsDbContext>(services);
            RemoveContext<PaymentsDbContext>(services);
            RemoveContext<PricingDbContext>(services);
            RemoveContext<InventoryDbContext>(services);
            RemoveContext<TravellerDbContext>(services);

            AddContext<BookingDbContext>(
                services,
                connection,
                typeof(BookingDbContext));
            AddContext<CatalogueDbContext>(
                services,
                connection,
                typeof(CatalogueDbContext));
            AddContext<OperatorsDbContext>(
                services,
                connection,
                typeof(OperatorsDbContext));
            AddContext<PaymentsDbContext>(
                services,
                connection,
                typeof(PaymentsDbContext));
            AddContext<PricingDbContext>(
                services,
                connection,
                typeof(PricingDbContext));
            AddContext<InventoryDbContext>(
                services,
                connection,
                typeof(InventoryDbContext));
            AddContext<TravellerDbContext>(
                services,
                connection,
                typeof(TravellerDbContext));

            services.RemoveAll<RazorpayPaymentProvider>();
            services.RemoveAll<IPaymentProviderGateway>();
            services.RemoveAll<IPaymentCheckoutCallbackVerifier>();
            services.RemoveAll<IPaymentProviderEventVerifier>();
            services.Configure<RazorpayOptions>(options =>
            {
                options.KeyId = "rzp_test_noorpath";
                options.KeySecret = "not-used-by-test-provider";
                options.WebhookSecret = "not-used-by-test-provider";
                options.CheckoutScriptUri =
                    new Uri("https://checkout.example.test/v1/checkout.js");
            });
            services.AddSingleton<TestPaymentProvider>();
            services.AddSingleton<IPaymentProviderGateway>(services =>
                services.GetRequiredService<TestPaymentProvider>());
            services.AddSingleton<IPaymentCheckoutCallbackVerifier>(services =>
                services.GetRequiredService<TestPaymentProvider>());
            services.AddSingleton<IPaymentProviderEventVerifier>(services =>
                services.GetRequiredService<TestPaymentProvider>());
        });
    }

    public HttpClient CreateIdentityClient(string accountId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-NoorPath-Test-Identity", accountId);
        return client;
    }

    private static void RemoveContext<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<TContext>();
    }

    private static void AddContext<TContext>(
        IServiceCollection services,
        string connectionString,
        Type migrationsType)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(
                    migrationsType.Assembly.FullName)));
    }

    private static async Task SeedOperatorsAsync(
        OperatorsDbContext db,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var approved = new OperatorRecord
        {
            Id = "operator-vs09",
            DisplayName = "NoorPath VS-09 Test Operator",
            State = OperatorState.Approved,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var membership = new OperatorMembershipRecord
        {
            Id = Guid.NewGuid(),
            OperatorId = approved.Id,
            AccountId = AuthorIdentity,
            Status = MembershipStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.AddRange(approved, membership);
        db.MembershipPermissions.Add(new OperatorMembershipPermissionRecord
        {
            Id = Guid.NewGuid(),
            MembershipId = membership.Id,
            Permission = OperatorPermissions.AdminAccess
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TestPaymentProvider :
    IPaymentProviderGateway,
    IPaymentCheckoutCallbackVerifier,
    IPaymentProviderEventVerifier
{
    public string ProviderName => "razorpay";

    public Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PaymentPolicy.ValidateAmount(request.Currency, request.Amount);
        return Task.FromResult(new PaymentCheckoutSession(
            ProviderName,
            $"order_{request.PaymentAttemptId:N}",
            "rzp_test_noorpath",
            decimal.ToInt64(request.Amount * 100m),
            request.Currency,
            new Uri("https://checkout.example.test/v1/checkout.js"),
            request.ExpiresAtUtc));
    }

    public bool Verify(PaymentCheckoutCallback callback) =>
        string.Equals(
            callback.Signature,
            SignatureFor(
                callback.ProviderSessionId,
                callback.ProviderPaymentId),
            StringComparison.Ordinal);

    public ValueTask<PaymentProviderEvent?> VerifyAsync(
        ReadOnlyMemory<byte> payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryHeader(headers, "X-Test-Signature", out var signature)
            || !string.Equals(signature, "valid", StringComparison.Ordinal)
            || !TryHeader(headers, "x-razorpay-event-id", out var eventId))
        {
            throw new CryptographicException("Test payment webhook is not authenticated.");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = root.GetProperty("eventType").GetString()!;
        var requestedState = Enum.Parse<PaymentAttemptState>(
            root.GetProperty("state").GetString()!,
            ignoreCase: true);
        var orderId = root.GetProperty("orderId").GetString()!;
        var paymentId = root.TryGetProperty("paymentId", out var payment)
            ? payment.GetString()
            : null;
        var occurredAt = root.TryGetProperty("occurredAtUtc", out var occurred)
            ? occurred.GetDateTimeOffset()
            : DateTimeOffset.UtcNow;
        var payloadHash = Convert.ToHexString(SHA256.HashData(payload.Span));

        return ValueTask.FromResult<PaymentProviderEvent?>(new PaymentProviderEvent(
            ProviderName,
            eventId,
            orderId,
            paymentId,
            eventType,
            requestedState,
            payloadHash,
            "test-key",
            occurredAt));
    }

    public static string SignatureFor(string orderId, string paymentId) =>
        $"signed:{orderId}:{paymentId}";

    private static bool TryHeader(
        IReadOnlyDictionary<string, string> headers,
        string name,
        out string value)
    {
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
