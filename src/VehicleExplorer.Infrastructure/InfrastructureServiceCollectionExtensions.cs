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
        });

        // The rest of the app asks for INhtsaClient and gets the cached decorator around the real client.
        services.AddTransient<INhtsaClient, CachedNhtsaClient>();

        return services;
    }
}
