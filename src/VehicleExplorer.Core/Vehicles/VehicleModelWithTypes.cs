namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// A model with the vehicle types NHTSA lists it under. Usually one type, sometimes several,
/// and empty when NHTSA lists the model under no type (for example some ATVs).
/// </summary>
public sealed record VehicleModelWithTypes(VehicleModel Model, IReadOnlyList<VehicleType> VehicleTypes);
