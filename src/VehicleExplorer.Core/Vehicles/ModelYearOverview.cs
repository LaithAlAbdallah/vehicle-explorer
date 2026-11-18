namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// Everything a make offers in one model year: its vehicle types with model counts, and every model with its types.
/// If some vehicle types could not be loaded from NHTSA they are listed in <see cref="UnavailableVehicleTypes"/>.
/// </summary>
public sealed record ModelYearOverview(
    VehicleMake Make,
    int ModelYear,
    IReadOnlyList<VehicleTypeModelCount> VehicleTypes,
    IReadOnlyList<VehicleModelWithTypes> Models,
    IReadOnlyList<VehicleType> UnavailableVehicleTypes)
{
    public bool IsComplete => UnavailableVehicleTypes.Count == 0;
}
