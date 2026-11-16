namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// The single seam between the application and the NHTSA vPIC API.
/// Implementations return domain types only, so NHTSA's response format never leaks into the core.
/// </summary>
public interface INhtsaClient
{
    Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleModel>> GetModelsAsync(int makeId, int modelYear, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleModel>> GetModelsByVehicleTypeAsync(
        int makeId,
        int modelYear,
        string vehicleTypeName,
        CancellationToken cancellationToken);
}
