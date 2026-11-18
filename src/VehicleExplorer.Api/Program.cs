using VehicleExplorer.Api.BackgroundServices;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<MakeSearchIndex>();
builder.Services.AddHostedService<MakeSearchIndexWarmUpService>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
