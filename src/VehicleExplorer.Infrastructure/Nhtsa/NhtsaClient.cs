using System.Net.Http.Json;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Infrastructure.Nhtsa.Responses;

namespace VehicleExplorer.Infrastructure.Nhtsa;

/// <summary>
/// Adapter over the NHTSA vPIC API: calls its endpoints and maps the responses to domain types.
/// Items without a name are skipped. Every upstream failure surfaces as <see cref="NhtsaUnavailableException"/>.
/// </summary>
public sealed class NhtsaClient(HttpClient httpClient) : INhtsaClient
{
    public async Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken)
    {
        var items = await GetResultsAsync<NhtsaMakeItem>("getallmakes?format=json", cancellationToken);
        return
        [
            .. items
                .Where(item => !string.IsNullOrWhiteSpace(item.MakeName))
                .Select(item => new VehicleMake(item.MakeId, item.MakeName!.Trim())),
        ];
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken)
    {
        var items = await GetResultsAsync<NhtsaVehicleTypeItem>(
            $"GetVehicleTypesForMakeId/{makeId}?format=json",
            cancellationToken);
        return
        [
            .. items
                .Where(item => !string.IsNullOrWhiteSpace(item.VehicleTypeName))
                .Select(item => new VehicleType(item.VehicleTypeName!.Trim())),
        ];
    }

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(int makeId, int modelYear, CancellationToken cancellationToken) =>
        GetModelsAsync($"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{modelYear}?format=json", cancellationToken);

    public Task<IReadOnlyList<VehicleModel>> GetModelsByVehicleTypeAsync(
        int makeId,
        int modelYear,
        string vehicleTypeName,
        CancellationToken cancellationToken) =>
        GetModelsAsync(
            $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{modelYear}/vehicletype/{Uri.EscapeDataString(vehicleTypeName)}?format=json",
            cancellationToken);

    private async Task<IReadOnlyList<VehicleModel>> GetModelsAsync(string requestUri, CancellationToken cancellationToken)
    {
        var items = await GetResultsAsync<NhtsaModelItem>(requestUri, cancellationToken);
        return
        [
            .. items
                .Where(item => !string.IsNullOrWhiteSpace(item.ModelName))
                .Select(item => new VehicleModel(item.ModelId, item.ModelName!.Trim())),
        ];
    }

    private async Task<IReadOnlyList<TItem>> GetResultsAsync<TItem>(string requestUri, CancellationToken cancellationToken)
    {
        NhtsaResponse<TItem>? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<NhtsaResponse<TItem>>(requestUri, cancellationToken);
        }
        // Error status or network error, bad JSON, the resilience timeout, or the open circuit breaker.
        // A cancelled request (OperationCanceledException) is not a failure, so it passes through unchanged.
        catch (Exception exception) when (exception
            is HttpRequestException or JsonException or TimeoutRejectedException or BrokenCircuitException)
        {
            throw new NhtsaUnavailableException($"NHTSA request '{requestUri}' failed.", exception);
        }

        return response?.Results ?? [];
    }
}
