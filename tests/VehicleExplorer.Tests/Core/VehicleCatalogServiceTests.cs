using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Core;

public sealed class VehicleCatalogServiceTests
{
    private const int HondaMakeId = 474;
    private const int ModelYear = 2015;

    private static readonly VehicleType _passengerCar = new("Passenger Car");
    private static readonly VehicleType _multipurposePassengerVehicle = new("Multipurpose Passenger Vehicle (MPV)");
    private static readonly VehicleType _motorcycle = new("Motorcycle");

    private static readonly VehicleModel _accord = new(1861, "Accord");
    private static readonly VehicleModel _crosstour = new(1862, "Crosstour");
    private static readonly VehicleModel _trx = new(1863, "TRX250X");

    private readonly FakeNhtsaClient _nhtsaClient = new();

    public VehicleCatalogServiceTests()
    {
        _nhtsaClient.Makes.Add(new VehicleMake(HondaMakeId, "HONDA"));
        _nhtsaClient.VehicleTypes.AddRange([_passengerCar, _multipurposePassengerVehicle, _motorcycle]);

        // Crosstour is listed under two types; the TRX250X (an ATV) under none; no motorcycles this year.
        _nhtsaClient.Models.AddRange([_trx, _crosstour, _accord]);
        _nhtsaClient.ModelsByVehicleTypeName[_passengerCar.Name] = [_accord, _crosstour];
        _nhtsaClient.ModelsByVehicleTypeName[_multipurposePassengerVehicle.Name] = [_crosstour];
    }

    [Fact]
    public async Task GetModelYearOverview_UnknownMake_ReturnsNull()
    {
        var overview = await GetOverviewAsync(makeId: 999);

        Assert.Null(overview);
    }

    [Fact]
    public async Task GetModelYearOverview_ModelInSeveralTypes_ListsModelOnceWithAllItsTypes()
    {
        var overview = await GetOverviewAsync();

        var crosstour = Assert.Single(overview!.Models, model => model.Model == _crosstour);
        Assert.Equal([_passengerCar, _multipurposePassengerVehicle], crosstour.VehicleTypes);
    }

    [Fact]
    public async Task GetModelYearOverview_ModelInNoType_ListsModelWithoutTypes()
    {
        var overview = await GetOverviewAsync();

        var trx = Assert.Single(overview!.Models, model => model.Model == _trx);
        Assert.Empty(trx.VehicleTypes);
    }

    [Fact]
    public async Task GetModelYearOverview_AllTypesLoaded_CountsModelsPerTypeAndLeavesOutEmptyTypes()
    {
        var overview = await GetOverviewAsync();

        Assert.Equal(
            [new VehicleTypeModelCount(_passengerCar, 2), new VehicleTypeModelCount(_multipurposePassengerVehicle, 1)],
            overview!.VehicleTypes);
        Assert.True(overview.IsComplete);
    }

    [Fact]
    public async Task GetModelYearOverview_AllTypesLoaded_SortsModelsByName()
    {
        var overview = await GetOverviewAsync();

        Assert.Equal([_accord, _crosstour, _trx], overview!.Models.Select(model => model.Model));
    }

    [Fact]
    public async Task GetModelYearOverview_OneVehicleTypeFails_ReturnsPartialOverview()
    {
        _nhtsaClient.FailingVehicleTypeNames.Add(_multipurposePassengerVehicle.Name);

        var overview = await GetOverviewAsync();

        Assert.False(overview!.IsComplete);
        Assert.Equal([_multipurposePassengerVehicle], overview.UnavailableVehicleTypes);
        Assert.Equal([new VehicleTypeModelCount(_passengerCar, 2)], overview.VehicleTypes);
        Assert.Equal(3, overview.Models.Count);
    }

    [Fact]
    public async Task GetModelYearOverview_FullModelListFails_ThrowsNhtsaUnavailableException()
    {
        _nhtsaClient.IsModelListFailing = true;

        await Assert.ThrowsAsync<NhtsaUnavailableException>(() => GetOverviewAsync());
    }

    private async Task<ModelYearOverview?> GetOverviewAsync(int makeId = HondaMakeId)
    {
        var vehicleCatalogService = new VehicleCatalogService(_nhtsaClient, new MakeSearchIndex());
        return await vehicleCatalogService.GetModelYearOverviewAsync(makeId, ModelYear, TestContext.Current.CancellationToken);
    }
}
