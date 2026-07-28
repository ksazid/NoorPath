using NoorPath.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new HealthResponse("Healthy")));
app.MapGet("/health/ready", () => Results.Ok(new HealthResponse("Ready")));

app.Run();

public partial class Program;

