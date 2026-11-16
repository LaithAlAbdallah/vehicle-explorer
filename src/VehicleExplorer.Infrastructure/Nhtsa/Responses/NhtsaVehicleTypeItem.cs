using System.Text.Json.Serialization;

namespace VehicleExplorer.Infrastructure.Nhtsa.Responses;

internal sealed record NhtsaVehicleTypeItem(
    [property: JsonPropertyName("VehicleTypeName")] string? VehicleTypeName);
