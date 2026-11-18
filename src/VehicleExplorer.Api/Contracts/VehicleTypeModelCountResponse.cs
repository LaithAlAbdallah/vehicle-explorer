using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Contracts;

/// <summary>A vehicle type and its model count, for example { "name": "Passenger Car", "modelCount": 4 }.</summary>
public sealed record VehicleTypeModelCountResponse(string Name, int ModelCount)
{
    public static VehicleTypeModelCountResponse From(VehicleTypeModelCount vehicleTypeModelCount) =>
        new(vehicleTypeModelCount.VehicleType.Name, vehicleTypeModelCount.ModelCount);
}
