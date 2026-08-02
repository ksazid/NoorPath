using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.BuildingBlocks;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Traveller.Infrastructure;
using NoorPath.Documents;
using NoorPath.Documents.Infrastructure;
using Amazon.S3;
using NoorPath.Visa.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("NoorPath") ?? throw new InvalidOperationException("ConnectionStrings:NoorPath is required.");
builder.Services.AddDbContext<BookingDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(BookingDbContext).Assembly.FullName)));
builder.Services.AddDbContext<CatalogueDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(CatalogueDbContext).Assembly.FullName)));
builder.Services.AddDbContext<OperatorsDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(OperatorsDbContext).Assembly.FullName)));
builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName)));
builder.Services.AddDbContext<PricingDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(PricingDbContext).Assembly.FullName)));
builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName)));
builder.Services.AddDbContext<TravellerDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(TravellerDbContext).Assembly.FullName)));
builder.Services.AddDbContext<DocumentsDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(DocumentsDbContext).Assembly.FullName)));
builder.Services.AddDbContext<VisaDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(VisaDbContext).Assembly.FullName)));
builder.Services.AddOptions<DocumentStorageOptions>().Bind(builder.Configuration.GetSection(DocumentStorageOptions.SectionName));
var documentsEnabled = builder.Configuration.GetValue<bool>("Documents:ProductionEnabled");
if (documentsEnabled)
{
    builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
    builder.Services.AddScoped<IPrivateDocumentStorage, S3DocumentStorage>();
    builder.Services.AddHostedService<DocumentRetentionWorker>();
}
else builder.Services.AddScoped<IPrivateDocumentStorage, DisabledDocumentStorage>();
builder.Services.AddScoped<IMalwareScanner, ClamAvScanner>();
builder.Services.AddScoped<IBookingCheckoutService, BookingCheckoutService>();
builder.Services.AddScoped<ConfirmationService>();
builder.Services.AddScoped<IOperatorAccess>(services => services.GetRequiredService<OperatorsDbContext>());
builder.Services.AddScoped<IOperatorPublicationEligibility>(services => services.GetRequiredService<OperatorsDbContext>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<InventoryHoldOptions>()
    .Bind(builder.Configuration.GetSection("InventoryHold"))
    .Validate(
        options => options.Lifetime > TimeSpan.Zero && options.Lifetime <= TimeSpan.FromMinutes(30),
        "InventoryHold:Lifetime must be greater than zero and no longer than 30 minutes.")
    .ValidateOnStart();
builder.Services.AddOptions<RazorpayOptions>()
    .Bind(builder.Configuration.GetSection(RazorpayOptions.SectionName));
builder.Services.AddHttpClient("Razorpay", (services, client) =>
{
    client.BaseAddress = services.GetRequiredService<IOptions<RazorpayOptions>>()
        .Value.ApiBaseAddress;
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddTransient(services => new RazorpayPaymentProvider(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("Razorpay"),
    services.GetRequiredService<IOptions<RazorpayOptions>>(),
    services.GetRequiredService<TimeProvider>()));
builder.Services.AddTransient<IPaymentProviderGateway>(services =>
    services.GetRequiredService<RazorpayPaymentProvider>());
builder.Services.AddTransient<IPaymentCheckoutCallbackVerifier>(services =>
    services.GetRequiredService<RazorpayPaymentProvider>());
builder.Services.AddTransient<IPaymentProviderEventVerifier>(services =>
    services.GetRequiredService<RazorpayPaymentProvider>());
builder.Services.AddNoorPathAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<OperatorsDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CatalogueDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<PricingDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<TravellerDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DocumentsDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<VisaDbContext>().Database.MigrateAsync();
}
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseNoorPathAuthenticationErrors();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new HealthResponse("Healthy")));
app.MapGet("/health/ready", async (OperatorsDbContext database, CancellationToken cancellationToken) =>
    await database.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new HealthResponse("Ready"))
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapOperatorAccess();
app.MapAccountAccess();
app.MapCatalogueAuthoring();
app.MapCommercialAuthoring();
app.MapPaymentPlanAuthoring();
app.MapPublicationReview();
app.MapPublicDiscovery();
app.MapPublicPackageDetails();
app.MapTravellerQuotes();
app.MapInventoryHolds();
app.MapBookings();
app.MapPayments();
app.MapConfirmations();
app.MapMyJourney();
app.MapDocuments();
app.MapVisa();
app.MapOperationalSupport();
app.Run();

public partial class Program;
