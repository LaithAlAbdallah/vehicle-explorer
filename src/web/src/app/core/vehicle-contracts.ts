// The API's JSON contracts (see the C# records in VehicleExplorer.Api/Contracts). Names follow the plan's §9a.

export interface VehicleMake {
  id: number;
  name: string;
}

export interface VehicleTypeModelCount {
  name: string;
  modelCount: number;
}

export interface VehicleModel {
  id: number;
  name: string;
  /** Official NHTSA vehicle type names; empty when NHTSA lists the model under no type. */
  vehicleTypes: string[];
}

export interface ModelYearOverview {
  make: VehicleMake;
  modelYear: number;
  vehicleTypes: VehicleTypeModelCount[];
  models: VehicleModel[];
  isComplete: boolean;
  unavailableVehicleTypes: string[];
}
