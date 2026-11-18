using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Contracts;

/// <summary>A vehicle make as returned by the API.</summary>
public sealed record VehicleMakeResponse(int Id, string Name)
{
    public static VehicleMakeResponse From(VehicleMake make) => new(make.Id, make.Name);
}
