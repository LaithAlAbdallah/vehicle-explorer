namespace VehicleExplorer.Core.Vehicles;

/// <summary>A vehicle make (brand) as registered with NHTSA, for example HONDA.</summary>
public sealed record VehicleMake(int Id, string Name);
