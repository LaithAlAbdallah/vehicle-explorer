namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// An official NHTSA vehicle type, for example "Passenger Car" or "Multipurpose Passenger Vehicle (MPV)".
/// Identified by its name, which is also what NHTSA accepts when filtering models by type.
/// </summary>
public sealed record VehicleType(string Name);
