namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// Builds the model year overview. NHTSA has no vehicle type per model, so the service asks for the full model list
/// and, in parallel, the model list of each vehicle type, then merges them: N vehicle types cost N + 2 NHTSA calls.
/// </summary>
public sealed class VehicleCatalogService(INhtsaClient nhtsaClient, MakeSearchIndex makeSearchIndex)
{
    /// <summary>
    /// Returns null if the make does not exist (NHTSA itself returns an empty list, not an error).
    /// If the full model list fails the call fails; if only some vehicle types fail, the overview is partial.
    /// </summary>
    public async Task<ModelYearOverview?> GetModelYearOverviewAsync(int makeId, int modelYear, CancellationToken cancellationToken)
    {
        await makeSearchIndex.EnsureLoadedAsync(nhtsaClient, cancellationToken);
        var make = makeSearchIndex.FindMake(makeId);
        if (make is null)
        {
            return null;
        }

        var vehicleTypes = await nhtsaClient.GetVehicleTypesAsync(makeId, cancellationToken);

        var allModelsTask = nhtsaClient.GetModelsAsync(makeId, modelYear, cancellationToken);
        var modelsByVehicleTypeTasks = vehicleTypes
            .Select(vehicleType => TryGetModelsByVehicleTypeAsync(makeId, modelYear, vehicleType, cancellationToken))
            .ToList();

        var allModels = await allModelsTask;
        var modelsByVehicleType = await Task.WhenAll(modelsByVehicleTypeTasks);

        return Merge(make, modelYear, allModels, modelsByVehicleType);
    }

    /// <summary>
    /// Returns null if NHTSA fails, so one failing vehicle type only makes the overview partial.
    /// Any other exception (a bug, a cancelled request) is not caught.
    /// </summary>
    private async Task<(VehicleType VehicleType, IReadOnlyList<VehicleModel>? Models)> TryGetModelsByVehicleTypeAsync(
        int makeId,
        int modelYear,
        VehicleType vehicleType,
        CancellationToken cancellationToken)
    {
        try
        {
            var models = await nhtsaClient.GetModelsByVehicleTypeAsync(makeId, modelYear, vehicleType.Name, cancellationToken);
            return (vehicleType, models);
        }
        catch (NhtsaUnavailableException)
        {
            return (vehicleType, null);
        }
    }

    private static ModelYearOverview Merge(
        VehicleMake make,
        int modelYear,
        IReadOnlyList<VehicleModel> allModels,
        IReadOnlyList<(VehicleType VehicleType, IReadOnlyList<VehicleModel>? Models)> modelsByVehicleType)
    {
        var loadedVehicleTypes = modelsByVehicleType
            .Where(result => result.Models is not null)
            .Select(result => (result.VehicleType, ModelIds: result.Models!.Select(model => model.Id).ToHashSet()))
            .ToList();

        // The full list is the source of truth: each model appears once, with the types whose lists contain it.
        var models = allModels
            .DistinctBy(model => model.Id)
            .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .Select(model => new VehicleModelWithTypes(
                model,
                [.. loadedVehicleTypes.Where(loaded => loaded.ModelIds.Contains(model.Id)).Select(loaded => loaded.VehicleType)]))
            .ToList();

        // Types with no models in this model year are left out (for example no motorcycles that year).
        var vehicleTypeModelCounts = loadedVehicleTypes
            .Select(loaded => new VehicleTypeModelCount(
                loaded.VehicleType,
                models.Count(model => model.VehicleTypes.Contains(loaded.VehicleType))))
            .Where(vehicleTypeModelCount => vehicleTypeModelCount.ModelCount > 0)
            .ToList();

        var unavailableVehicleTypes = modelsByVehicleType
            .Where(result => result.Models is null)
            .Select(result => result.VehicleType)
            .ToList();

        return new ModelYearOverview(make, modelYear, vehicleTypeModelCounts, models, unavailableVehicleTypes);
    }
}
