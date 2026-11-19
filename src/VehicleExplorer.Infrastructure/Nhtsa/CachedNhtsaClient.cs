using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Infrastructure.Nhtsa;

/// <summary>
/// Decorator that caches every NHTSA call separately, including each call of the overview's fan-out.
/// HybridCache also makes concurrent requests for the same key share one NHTSA call (stampede protection).
/// Errors are never cached: if NHTSA fails, nothing is stored and the next request tries again.
/// </summary>
public sealed class CachedNhtsaClient(NhtsaClient nhtsaClient, HybridCache cache, IOptions<NhtsaCacheOptions> options)
    : INhtsaClient
{
    private readonly NhtsaCacheOptions _options = options.Value;

    /// <summary>
    /// Not cached here: the 12,000 makes are held by the make search index, and they are larger
    /// than HybridCache's default 1 MB entry limit.
    /// </summary>
    public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) =>
        nhtsaClient.GetMakesAsync(cancellationToken);

    public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken) =>
        GetOrFetchAsync(
            $"nhtsa:vehicle-types:{makeId}",
            _options.VehicleTypesDuration,
            token => nhtsaClient.GetVehicleTypesAsync(makeId, token),
            cancellationToken);

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(int makeId, int modelYear, CancellationToken cancellationToken) =>
        GetOrFetchAsync(
            $"nhtsa:models:{makeId}:{modelYear}",
            _options.ModelsDuration,
            token => nhtsaClient.GetModelsAsync(makeId, modelYear, token),
            cancellationToken);

    public Task<IReadOnlyList<VehicleModel>> GetModelsByVehicleTypeAsync(
        int makeId,
        int modelYear,
        string vehicleTypeName,
        CancellationToken cancellationToken) =>
        GetOrFetchAsync(
            $"nhtsa:models:{makeId}:{modelYear}:{vehicleTypeName}",
            _options.ModelsDuration,
            token => nhtsaClient.GetModelsByVehicleTypeAsync(makeId, modelYear, vehicleTypeName, token),
            cancellationToken);

    private async Task<IReadOnlyList<TItem>> GetOrFetchAsync<TItem>(
        string key,
        TimeSpan duration,
        Func<CancellationToken, Task<IReadOnlyList<TItem>>> fetchAsync,
        CancellationToken cancellationToken)
    {
        var isFetchedNow = false;
        var items = await cache.GetOrCreateAsync(
            key,
            async token =>
            {
                isFetchedNow = true;
                return await fetchAsync(token);
            },
            CreateEntryOptions(duration),
            cancellationToken: cancellationToken);

        // The duration is chosen before the answer is known, so a fresh empty answer is stored again
        // with the short duration. Only when fetched now: otherwise every hit would extend it. Not cancelled with the
        // request: if the user moves on right now, the empty answer must still not keep the long duration.
        if (isFetchedNow && items.Count == 0)
        {
            await cache.SetAsync(key, items, CreateEntryOptions(_options.EmptyResultDuration), cancellationToken: CancellationToken.None);
        }

        return items;
    }

    private static HybridCacheEntryOptions CreateEntryOptions(TimeSpan duration) =>
        new() { Expiration = duration, LocalCacheExpiration = duration };
}
