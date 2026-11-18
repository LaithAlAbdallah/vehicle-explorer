using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Contracts;

/// <summary>A model with its vehicle type names, for example { "id": 1861, "name": "Accord", "vehicleTypes": ["Passenger Car"] }.</summary>
public sealed record VehicleModelResponse(int Id, string Name, IReadOnlyList<string> VehicleTypes)
{
    public static VehicleModelResponse From(VehicleModelWithTypes modelWithTypes) => new(
        modelWithTypes.Model.Id,
        modelWithTypes.Model.Name,
        [.. modelWithTypes.VehicleTypes.Select(vehicleType => vehicleType.Name)]);
}
