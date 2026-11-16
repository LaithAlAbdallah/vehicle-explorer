namespace VehicleExplorer.Infrastructure.Nhtsa.Responses;

/// <summary>The envelope every NHTSA vPIC JSON response is wrapped in (only the part we use).</summary>
internal sealed record NhtsaResponse<TItem>(IReadOnlyList<TItem>? Results);
