using System.Net;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Infrastructure.Nhtsa;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Infrastructure;

public sealed class NhtsaClientTests
{
    [Fact]
    public async Task GetMakes_ValidResponse_MapsMakesAndSkipsNamelessItems()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("""
            {"Count":3,"Results":[
              {"Make_ID":474,"Make_Name":" HONDA "},
              {"Make_ID":1,"Make_Name":null},
              {"Make_ID":2,"Make_Name":"  "}
            ]}
            """);

        var makes = await CreateClient(handler).GetMakesAsync(TestContext.Current.CancellationToken);

        Assert.Equal([new VehicleMake(474, "HONDA")], makes);
    }

    [Fact]
    public async Task GetVehicleTypes_ValidResponse_MapsVehicleTypeNames()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("""
            {"Results":[{"VehicleTypeId":2,"VehicleTypeName":"Passenger Car"},{"VehicleTypeId":1,"VehicleTypeName":"Motorcycle"}]}
            """);

        var vehicleTypes = await CreateClient(handler).GetVehicleTypesAsync(474, TestContext.Current.CancellationToken);

        Assert.Equal([new VehicleType("Passenger Car"), new VehicleType("Motorcycle")], vehicleTypes);
        Assert.EndsWith("GetVehicleTypesForMakeId/474?format=json", handler.RequestUris.Single().AbsoluteUri);
    }

    [Fact]
    public async Task GetModelsByVehicleType_NameWithSpacesAndBrackets_EscapesNameInUrl()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("""
            {"Results":[{"Model_ID":1861,"Model_Name":"Pilot"}]}
            """);

        var models = await CreateClient(handler).GetModelsByVehicleTypeAsync(
            474,
            2015,
            "Multipurpose Passenger Vehicle (MPV)",
            TestContext.Current.CancellationToken);

        Assert.Equal([new VehicleModel(1861, "Pilot")], models);
        Assert.EndsWith(
            "GetModelsForMakeIdYear/makeId/474/modelyear/2015/vehicletype/Multipurpose%20Passenger%20Vehicle%20%28MPV%29?format=json",
            handler.RequestUris.Single().AbsoluteUri);
    }

    [Fact]
    public async Task GetModels_ResultsMissing_ReturnsEmptyList()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("""{"Count":0}""");

        var models = await CreateClient(handler).GetModelsAsync(474, 2015, TestContext.Current.CancellationToken);

        Assert.Empty(models);
    }

    [Fact]
    public async Task GetMakes_ServerError_ThrowsNhtsaUnavailableException()
    {
        var handler = StubHttpMessageHandler.RespondingWithStatus(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<NhtsaUnavailableException>(
            () => CreateClient(handler).GetMakesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMakes_ResponseIsNotJson_ThrowsNhtsaUnavailableException()
    {
        var handler = StubHttpMessageHandler.RespondingWithJson("<html>Too many requests</html>");

        await Assert.ThrowsAsync<NhtsaUnavailableException>(
            () => CreateClient(handler).GetMakesAsync(TestContext.Current.CancellationToken));
    }

    internal static NhtsaClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://nhtsa.test/api/vehicles/") });
}
