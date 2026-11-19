using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Infrastructure.Nhtsa;

namespace VehicleExplorer.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NhtsaOptions>(configuration.GetSection(NhtsaOptions.SectionName));
        services.Configure<NhtsaCacheOptions>(configuration.GetSection(NhtsaCacheOptions.SectionName));

        services.AddHybridCache();

        services.AddHttpClient<NhtsaClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NhtsaOptions>>().Value;
            httpClient.BaseAddress = options.BaseAddress;
        })
        // Microsoft's standard pipeline: retries with exponential backoff and jitter (on 5xx, 408, 429 and network
        // errors), 10 s per attempt, a total timeout, and a circuit breaker for a long outage. A person is waiting in
        // the UI, so it gives up sooner than the defaults (3 retries from 2 s, 30 s in total).
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
        });

        // The rest of the app asks for INhtsaClient and gets the cached decorator around the real client.
        services.AddTransient<INhtsaClient, CachedNhtsaClient>();

        return services;
    }
}
