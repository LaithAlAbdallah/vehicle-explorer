namespace VehicleExplorer.Core.Vehicles;

/// <summary>A vehicle type and how many of the make's models in the model year have it.</summary>
public sealed record VehicleTypeModelCount(VehicleType VehicleType, int ModelCount);
