using System.Net;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Infrastructure.Nhtsa;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Infrastructure;

public sealed class CachedNhtsaClientTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider = new ServiceCollection().AddHybridCache().Services.BuildServiceProvider();

    [Fact]
    public async Task GetVehicleTypes_CalledTwice_CallsNhtsaOnce()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("""{"Results":[{"VehicleTypeName":"Passenger Car"}]}""");
        var cachedNhtsaClient = CreateCachedClient(handler);

        await cachedNhtsaClient.GetVehicleTypesAsync(474, TestContext.Current.CancellationToken);
        var vehicleTypes = await cachedNhtsaClient.GetVehicleTypesAsync(474, TestContext.Current.CancellationToken);

        Assert.Equal([new VehicleType("Passenger Car")], vehicleTypes);
        Assert.Single(handler.RequestUris);
    }

    [Fact]
    public async Task GetModelsByVehicleType_DifferentVehicleTypes_CachesEachSeparately()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("""{"Results":[{"Model_ID":1,"Model_Name":"Accord"}]}""");
        var cachedNhtsaClient = CreateCachedClient(handler);

        await cachedNhtsaClient.GetModelsByVehicleTypeAsync(474, 2015, "Passenger Car", TestContext.Current.CancellationToken);
        await cachedNhtsaClient.GetModelsByVehicleTypeAsync(474, 2015, "Motorcycle", TestContext.Current.CancellationToken);
        await cachedNhtsaClient.GetModelsByVehicleTypeAsync(474, 2015, "Passenger Car", TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task GetModels_NhtsaFails_DoesNotCacheTheFailure()
    {
        var handler = StubHttpMessageHandler.RespondingWithStatus(HttpStatusCode.ServiceUnavailable);
        var cachedNhtsaClient = CreateCachedClient(handler);

        await Assert.ThrowsAsync<NhtsaUnavailableException>(
            () => cachedNhtsaClient.GetModelsAsync(474, 2015, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NhtsaUnavailableException>(
            () => cachedNhtsaClient.GetModelsAsync(474, 2015, TestContext.Current.CancellationToken));

        Assert.Equal(2, handler.RequestUris.Count);
    }

    public void Dispose() => _serviceProvider.Dispose();

    private CachedNhtsaClient CreateCachedClient(StubHttpMessageHandler handler) =>
        new(
            NhtsaClientTests.CreateClient(handler),
            _serviceProvider.GetRequiredService<HybridCache>(),
            Options.Create(new NhtsaCacheOptions()));
}
