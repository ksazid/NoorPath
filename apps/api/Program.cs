using Microsoft.EntityFrameworkCore;
using NoorPath.BuildingBlocks;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;
using NoorPath.Operators.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("NoorPath") ?? throw new InvalidOperationException("ConnectionStrings:NoorPath is required.");
builder.Services.AddDbContext<CatalogueDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(CatalogueDbContext).Assembly.FullName)));
builder.Services.AddDbContext<OperatorsDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(typeof(OperatorsDbContext).Assembly.FullName)));
builder.Services.AddScoped<IOperatorAccess>(services => services.GetRequiredService<OperatorsDbContext>());
builder.Services.AddNoorPathAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();
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
app.MapGet("/health/ready", () => Results.Ok(new HealthResponse("Ready")));
app.MapOperatorAccess();
app.MapCatalogueAuthoring();
app.Run();

public partial class Program;
