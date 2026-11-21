using VehicleExplorer.Api.BackgroundServices;
using VehicleExplorer.Api.Errors;
using VehicleExplorer.Api.RateLimiting;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<NhtsaUnavailableExceptionHandler>();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<MakeSearchIndex>();
builder.Services.AddScoped<VehicleCatalogService>();
builder.Services.AddHostedService<MakeSearchIndexWarmUpService>();

var app = builder.Build();

// Exceptions → 503 or 500 and empty error responses (such as an unknown route) → ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

// The Angular build is copied into wwwroot (see the Dockerfile), so one app serves the website and the API.
app.UseStaticFiles();

app.MapControllers().RequireRateLimiting(ApiRateLimitOptions.PolicyName);
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Every other address is an Angular page, except unknown /api addresses, which stay 404.
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
