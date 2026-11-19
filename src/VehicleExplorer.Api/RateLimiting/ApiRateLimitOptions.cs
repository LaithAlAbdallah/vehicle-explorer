namespace VehicleExplorer.Api.RateLimiting;

/// <summary>How many API requests one IP address may make per window. Override with the "ApiRateLimit" configuration section.</summary>
public sealed class ApiRateLimitOptions
{
    public const string SectionName = "ApiRateLimit";
    public const string PolicyName = "api";

    /// <summary>Enough for fast typing in the make search (one request per debounced keystroke), too few to hammer NHTSA.</summary>
    public int PermitLimit { get; init; } = 100;

    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
}
