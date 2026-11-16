using System.Text.Json.Serialization;

namespace VehicleExplorer.Infrastructure.Nhtsa.Responses;

internal sealed record NhtsaMakeItem(
    [property: JsonPropertyName("Make_ID")] int MakeId,
    [property: JsonPropertyName("Make_Name")] string? MakeName);
