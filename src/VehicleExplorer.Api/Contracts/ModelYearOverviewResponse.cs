using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Contracts;

/// <summary>The model year overview as returned by the API. Vehicle types are identified by their official name.</summary>
public sealed record ModelYearOverviewResponse(
    VehicleMakeResponse Make,
    int ModelYear,
    IReadOnlyList<VehicleTypeModelCountResponse> VehicleTypes,
    IReadOnlyList<VehicleModelResponse> Models,
    bool IsComplete,
    IReadOnlyList<string> UnavailableVehicleTypes)
{
    public static ModelYearOverviewResponse From(ModelYearOverview overview) => new(
        VehicleMakeResponse.From(overview.Make),
        overview.ModelYear,
        [.. overview.VehicleTypes.Select(VehicleTypeModelCountResponse.From)],
        [.. overview.Models.Select(VehicleModelResponse.From)],
        overview.IsComplete,
        [.. overview.UnavailableVehicleTypes.Select(vehicleType => vehicleType.Name)]);
}
