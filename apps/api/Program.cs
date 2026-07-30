using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NoorPath.BuildingBlocks;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("NoorPath") ?? throw new InvalidOperationException("ConnectionStrings:NoorPath is required.");
builder.Services.AddDbContext<CatalogueDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(CatalogueDbContext).Assembly.FullName)));
builder.Services.AddDbContext<OperatorsDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(OperatorsDbContext).Assembly.FullName)));
builder.Services.AddScoped<IOperatorAccess>(services => services.GetRequiredService<OperatorsDbContext>());
builder.Services.AddNoorPathAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<IOperatorApproval>(_ => new TestOperatorApproval(builder.Configuration.GetSection("S02:ApprovedTestOperatorIds").Get<string[]>() ?? []));
builder.Services.AddRateLimiter(options => options.AddPolicy("public-catalogue", http => RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
var app = builder.Build();
app.UseRateLimiter();
app.Use(async (context, next) => { context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier; context.Response.Headers["X-Content-Type-Options"] = "nosniff"; context.Response.Headers["Referrer-Policy"] = "no-referrer"; await next(); });
app.UseNoorPathAuthenticationErrors();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new HealthResponse("Healthy")));
app.MapGet("/health/ready", () => Results.Ok(new HealthResponse("Ready")));
app.MapOperatorAccess();

var admin = app.MapGroup("/api/v1/admin");
admin.AddEndpointFilter(async (invocation, next) => invocation.HttpContext.Request.Headers["X-NoorPath-Admin"] == "s02-pilot-admin" ? await next(invocation) : Results.Problem(statusCode: 403, title: "Permission denied", detail: "No unpublished catalogue information is available."));
admin.MapPost("/batches", async (CreateBatch command, CatalogueDbContext db, IOperatorApproval operators, HttpContext http, ILogger<Program> log, CancellationToken cancellation) =>
{
    try
    {
        if (!operators.IsApprovedTestOperator(command.OperatorId)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["operatorId"] = ["Select an approved test operator."] }, statusCode: 422, title: "Review the batch details");
        var domain = new Batch(command);
        var package = new PackageRecord { Id = Guid.NewGuid(), OperatorId = command.OperatorId, OperatorName = command.OperatorName, Name = command.PackageName, Summary = command.Summary, Tier = command.Tier };
        var batch = new BatchRecord { Id = domain.Id, PackageId = package.Id, DepartureCity = command.DepartureCity, Route = command.Route, DepartureDate = command.DepartureDate, ReturnDate = command.ReturnDate, Capacity = command.Capacity, Availability = command.Availability };
        db.Add(package); db.Add(batch); db.Add(new PriceVersionRecord { Id = Guid.NewGuid(), BatchId = batch.Id, Currency = "INR", TotalStartingPrice = command.TotalPriceInr, EffectiveAt = DateTimeOffset.UtcNow });
        db.AddRange(domain.Details.Inclusions.Select((text, index) => new InclusionRecord { Id = Guid.NewGuid(), PackageId = package.Id, Position = index, Text = text }));
        await db.SaveChangesAsync(cancellation); log.LogInformation("Catalogue draft outcome={Outcome} batchId={BatchId} operatorId={OperatorId} correlationId={CorrelationId}", "created", batch.Id, package.OperatorId, http.TraceIdentifier);
        return Results.Created($"/api/v1/admin/batches/{batch.Id}", new { batch.Id, batch.Version, status = batch.Status.ToString() });
    }
    catch (BatchValidationException ex) { return Results.ValidationProblem(ex.Errors, statusCode: 422, title: "Review the batch details"); }
});
admin.MapPost("/batches/{id:guid}/publish", async (Guid id, PublishRequest request, CatalogueDbContext db, IOperatorApproval operators, HttpContext http, ILogger<Program> log, CancellationToken cancellation) =>
{
    var batch = await db.Batches.SingleOrDefaultAsync(x => x.Id == id, cancellation);
    if (batch is null) return Results.NotFound();
    var package = await db.Packages.SingleAsync(x => x.Id == batch.PackageId, cancellation);
    if (package.OperatorId != request.OperatorId) return Results.NotFound();
    if (!operators.IsApprovedTestOperator(package.OperatorId)) return Results.Conflict(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 409, Title = "Publication rejected", Detail = "The operator is not approved for publication." });
    if (batch.Status == BatchStatus.Published) return Results.Conflict(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 409, Title = "Publication rejected", Detail = "The batch is already published." });
    if (batch.Version != request.ExpectedVersion) return Results.Conflict(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 409, Title = "Publication rejected", Detail = "The draft changed. Refresh and review it again." });
    var priceIsEffective = await db.PriceVersions.AnyAsync(x => x.BatchId == id && x.Currency == "INR" && x.TotalStartingPrice > 0 && x.EffectiveAt <= DateTimeOffset.UtcNow, cancellation);
    if (!priceIsEffective) return Results.Conflict(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 409, Title = "Publication rejected", Detail = "An effective INR price is required." });
    await using var transaction = await db.Database.BeginTransactionAsync(cancellation);
    batch.Status = BatchStatus.Published; batch.PublishedAt = DateTimeOffset.UtcNow; batch.Version++;
    db.PublicationAudits.Add(new PublicationAuditRecord { Id = Guid.NewGuid(), BatchId = batch.Id, Actor = "noorpath-admin", CorrelationId = http.TraceIdentifier, PreviousStatus = BatchStatus.Draft.ToString(), NewStatus = BatchStatus.Published.ToString(), ExpectedVersion = request.ExpectedVersion, Timestamp = batch.PublishedAt.Value });
    try { await db.SaveChangesAsync(cancellation); await transaction.CommitAsync(cancellation); }
    catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(cancellation); return Results.Conflict(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 409, Title = "Publication rejected", Detail = "The draft changed. Refresh and review it again." }); }
    log.LogInformation("Catalogue publication outcome={Outcome} batchId={BatchId} operatorId={OperatorId} correlationId={CorrelationId}", "published", batch.Id, package.OperatorId, http.TraceIdentifier);
    return Results.Ok(new { batch.Id, batch.Version, status = batch.Status.ToString(), batch.PublishedAt });
});
app.MapGet("/api/v1/batches", async (HttpContext http, CatalogueDbContext db, CancellationToken cancellation) =>
{
    var now = DateTimeOffset.UtcNow;
    var rows = await (from batch in db.Batches.AsNoTracking() join package in db.Packages.AsNoTracking() on batch.PackageId equals package.Id join price in db.PriceVersions.AsNoTracking() on batch.Id equals price.BatchId where batch.Status == BatchStatus.Published && batch.PublishedAt <= now && price.Currency == "INR" && price.EffectiveAt <= now orderby batch.DepartureDate select new { batch, package, price }).ToListAsync(cancellation);
    var packageIds = rows.Select(x => x.package.Id).ToArray();
    var inclusions = await db.Inclusions.AsNoTracking().Where(x => packageIds.Contains(x.PackageId)).OrderBy(x => x.Position).ToListAsync(cancellation);
    var result = rows.Select(x => new PublicBatch(x.batch.Id, x.package.OperatorName, true, x.package.Name, x.package.Summary, x.package.Tier, x.batch.DepartureCity, x.batch.Route, x.batch.DepartureDate, x.batch.ReturnDate, x.batch.ReturnDate.DayNumber - x.batch.DepartureDate.DayNumber, x.batch.Capacity, x.price.TotalStartingPrice, x.batch.Availability, inclusions.Where(i => i.PackageId == x.package.Id).Select(i => i.Text).ToArray()));
    http.Response.Headers.CacheControl = "public,max-age=60"; return Results.Ok(result);
}).RequireRateLimiting("public-catalogue");
app.Run();
public sealed record PublishRequest(int ExpectedVersion, string OperatorId);
public partial class Program;
