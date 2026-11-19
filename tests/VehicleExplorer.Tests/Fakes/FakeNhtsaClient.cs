using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Tests.Fakes;

/// <summary>
/// In-memory NHTSA for tests: returns the lists it is given, whatever make or model year is asked for,
/// and fails where told to.
/// </summary>
internal sealed class FakeNhtsaClient : INhtsaClient
{
    public List<VehicleMake> Makes { get; } = [];

    public List<VehicleType> VehicleTypes { get; } = [];

    public List<VehicleModel> Models { get; } = [];

    public Dictionary<string, List<VehicleModel>> ModelsByVehicleTypeName { get; } = [];

    public HashSet<string> FailingVehicleTypeNames { get; } = [];

    public bool IsModelListFailing { get; set; }

    public Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VehicleMake>>(Makes);

    public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VehicleType>>(VehicleTypes);

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(int makeId, int modelYear, CancellationToken cancellationToken) =>
        IsModelListFailing
            ? throw CreateUnavailableException()
            : Task.FromResult<IReadOnlyList<VehicleModel>>(Models);

    public Task<IReadOnlyList<VehicleModel>> GetModelsByVehicleTypeAsync(
        int makeId,
        int modelYear,
        string vehicleTypeName,
        CancellationToken cancellationToken)
    {
        if (FailingVehicleTypeNames.Contains(vehicleTypeName))
        {
            throw CreateUnavailableException();
        }

        return Task.FromResult<IReadOnlyList<VehicleModel>>(ModelsByVehicleTypeName.GetValueOrDefault(vehicleTypeName) ?? []);
    }

    private static NhtsaUnavailableException CreateUnavailableException() =>
        new("NHTSA is down (fake).", new HttpRequestException("Connection refused (fake)."));
}
