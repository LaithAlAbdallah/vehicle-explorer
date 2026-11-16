using System.Text.Json.Serialization;

namespace VehicleExplorer.Infrastructure.Nhtsa.Responses;

internal sealed record NhtsaModelItem(
    [property: JsonPropertyName("Model_ID")] int ModelId,
    [property: JsonPropertyName("Model_Name")] string? ModelName);
