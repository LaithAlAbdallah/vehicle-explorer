using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Api.Contracts;
using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/makes")]
public sealed class MakesController(MakeSearchIndex makeSearchIndex, INhtsaClient nhtsaClient) : ControllerBase
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 50;
    private const int MaxSearchLength = 50;

    /// <summary>Ranked make search, for example <c>GET /api/makes?search=hon&amp;limit=10</c>.</summary>
    /// <remarks>Invalid input returns 400 automatically ([ApiController] validates the attributes).</remarks>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleMakeResponse>>> SearchMakesAsync(
        [FromQuery, Required, StringLength(MaxSearchLength)] string search,
        [FromQuery, Range(1, MaxLimit)] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        // Normally loaded by the warm-up service; this covers NHTSA being down at startup.
        if (!makeSearchIndex.IsLoaded)
        {
            await makeSearchIndex.LoadAsync(nhtsaClient, cancellationToken);
        }

        return Ok(makeSearchIndex.Search(search, limit).Select(VehicleMakeResponse.From).ToList());
    }
}
