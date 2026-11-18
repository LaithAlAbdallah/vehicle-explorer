using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.BackgroundServices;

/// <summary>
/// Loads the make search index at startup and refreshes it every 24 hours.
/// If NHTSA is down it only logs a warning: the app stays up and the index loads on the first search.
/// </summary>
public sealed partial class MakeSearchIndexWarmUpService(
    MakeSearchIndex makeSearchIndex,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MakeSearchIndexWarmUpService> logger) : BackgroundService
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_refreshInterval);
        do
        {
            await LoadIndexAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task LoadIndexAsync(CancellationToken stoppingToken)
    {
        try
        {
            // The NHTSA client is a typed HttpClient (short-lived), so it is resolved from a scope
            // instead of being injected into this long-lived service.
            using var scope = serviceScopeFactory.CreateScope();
            var nhtsaClient = scope.ServiceProvider.GetRequiredService<INhtsaClient>();
            await makeSearchIndex.LoadAsync(nhtsaClient, stoppingToken);
            LogIndexLoaded(makeSearchIndex.Count);
        }
        // Only app shutdown may stop this loop. An HTTP timeout is also an OperationCanceledException,
        // so the filter checks the stopping token instead of the exception type.
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            LogIndexLoadFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Make search index loaded with {MakeCount} makes.")]
    private partial void LogIndexLoaded(int makeCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load the make search index; it will load on the first search.")]
    private partial void LogIndexLoadFailed(Exception exception);
}
