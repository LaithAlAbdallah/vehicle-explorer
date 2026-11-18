using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Api.Contracts;
using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/makes")]
public sealed class MakesController(
    MakeSearchIndex makeSearchIndex,
    VehicleCatalogService vehicleCatalogService,
    INhtsaClient nhtsaClient) : ControllerBase
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 50;
    private const int MaxSearchLength = 50;

    // NHTSA has no models before 1981 (the year the 17-character VIN became the standard), checked live.
    private const int MinModelYear = 1981;

    /// <summary>Ranked make search, for example <c>GET /api/makes?search=hon&amp;limit=10</c>.</summary>
    /// <remarks>Invalid input returns 400 automatically ([ApiController] validates the attributes).</remarks>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleMakeResponse>>> SearchMakesAsync(
        [FromQuery, Required, StringLength(MaxSearchLength)] string search,
        [FromQuery, Range(1, MaxLimit)] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        await makeSearchIndex.EnsureLoadedAsync(nhtsaClient, cancellationToken);
        return Ok(makeSearchIndex.Search(search, limit).Select(VehicleMakeResponse.From).ToList());
    }

    /// <summary>Vehicle types and models of a make in a model year, for example <c>GET /api/makes/474/model-years/2015</c>.</summary>
    [HttpGet("{makeId:int}/model-years/{modelYear:int}")]
    public async Task<ActionResult<ModelYearOverviewResponse>> GetModelYearOverviewAsync(
        [Range(1, int.MaxValue)] int makeId,
        int modelYear,
        CancellationToken cancellationToken)
    {
        // The upper bound moves with the calendar, so it cannot be an attribute. Next year's models are sold this year;
        // NHTSA answers any later year with its newest models, so later years are rejected.
        var maxModelYear = DateTime.UtcNow.Year + 1;
        if (modelYear < MinModelYear || modelYear > maxModelYear)
        {
            ModelState.AddModelError(nameof(modelYear), $"The model year must be between {MinModelYear} and {maxModelYear}.");
            return ValidationProblem(ModelState);
        }

        var overview = await vehicleCatalogService.GetModelYearOverviewAsync(makeId, modelYear, cancellationToken);
        return overview is null ? NotFound() : Ok(ModelYearOverviewResponse.From(overview));
    }
}
