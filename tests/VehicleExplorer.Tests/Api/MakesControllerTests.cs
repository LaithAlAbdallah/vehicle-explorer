using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Api;

/// <summary>Runs the whole API in memory (routing, validation, error handling, rate limiting) with a fake NHTSA.</summary>
public sealed class MakesControllerTests : IDisposable
{
    private readonly FakeNhtsaClient _nhtsaClient = new();
    private readonly WebApplicationFactory<Program> _factory;

    public MakesControllerTests()
    {
        _nhtsaClient.Makes.Add(new VehicleMake(474, "HONDA"));
        _nhtsaClient.VehicleTypes.Add(new VehicleType("Passenger Car"));
        _nhtsaClient.Models.Add(new VehicleModel(1861, "Accord"));
        _nhtsaClient.ModelsByVehicleTypeName["Passenger Car"] = [new VehicleModel(1861, "Accord")];

        _factory = CreateFactory(permitLimit: 100);
    }

    [Fact]
    public async Task SearchMakes_ValidSearch_Returns200WithMakes()
    {
        using var client = _factory.CreateClient();

        var makes = await client.GetFromJsonAsync<JsonElement>("/api/makes?search=hon", TestContext.Current.CancellationToken);

        Assert.Equal("HONDA", makes[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task SearchMakes_LimitAboveMaximum_Returns400()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/makes?search=hon&limit=51", TestContext.Current.CancellationToken);

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetModelYearOverview_KnownMake_Returns200WithOverview()
    {
        using var client = _factory.CreateClient();

        var overview = await client.GetFromJsonAsync<JsonElement>(
            "/api/makes/474/model-years/2015",
            TestContext.Current.CancellationToken);

        Assert.True(overview.GetProperty("isComplete").GetBoolean());
        Assert.Equal("Accord", overview.GetProperty("models")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetModelYearOverview_OneVehicleTypeFails_Returns200WithUnavailableVehicleTypes()
    {
        _nhtsaClient.FailingVehicleTypeNames.Add("Passenger Car");
        using var client = _factory.CreateClient();

        var overview = await client.GetFromJsonAsync<JsonElement>(
            "/api/makes/474/model-years/2015",
            TestContext.Current.CancellationToken);

        Assert.False(overview.GetProperty("isComplete").GetBoolean());
        Assert.Equal("Passenger Car", overview.GetProperty("unavailableVehicleTypes")[0].GetString());
        Assert.Equal("Accord", overview.GetProperty("models")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetModelYearOverview_UnknownMake_Returns404()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/makes/999/model-years/2015", TestContext.Current.CancellationToken);

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(1980)]
    [InlineData(2099)]
    public async Task GetModelYearOverview_ModelYearOutOfRange_Returns400(int modelYear)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync($"/api/makes/474/model-years/{modelYear}", TestContext.Current.CancellationToken);

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetModelYearOverview_NhtsaUnavailable_Returns503()
    {
        _nhtsaClient.IsModelListFailing = true;
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/makes/474/model-years/2015", TestContext.Current.CancellationToken);

        await AssertProblemDetailsAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task SearchMakes_OverRateLimit_Returns429WithRetryAfter()
    {
        using var factory = CreateFactory(permitLimit: 1);
        using var client = factory.CreateClient();

        using var allowedResponse = await client.GetAsync("/api/makes?search=hon", TestContext.Current.CancellationToken);
        using var rejectedResponse = await client.GetAsync("/api/makes?search=hon", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        await AssertProblemDetailsAsync(rejectedResponse, HttpStatusCode.TooManyRequests);
        Assert.NotNull(rejectedResponse.Headers.RetryAfter);
    }

    public void Dispose() => _factory.Dispose();

    private WebApplicationFactory<Program> CreateFactory(int permitLimit) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ApiRateLimit:PermitLimit", permitLimit.ToString(CultureInfo.InvariantCulture));
            builder.ConfigureTestServices(services => services.AddSingleton<INhtsaClient>(_nhtsaClient));
        });

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal((int)expectedStatusCode, problemDetails.GetProperty("status").GetInt32());
    }
}
