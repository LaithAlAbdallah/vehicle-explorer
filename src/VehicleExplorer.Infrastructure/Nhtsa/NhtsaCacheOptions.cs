namespace VehicleExplorer.Infrastructure.Nhtsa;

/// <summary>How long NHTSA answers are cached. Override with the "NhtsaCache" configuration section.</summary>
public sealed class NhtsaCacheOptions
{
    public const string SectionName = "NhtsaCache";

    /// <summary>A make's vehicle types almost never change.</summary>
    public TimeSpan VehicleTypesDuration { get; init; } = TimeSpan.FromHours(24);

    /// <summary>Model lists change a few times a year, when NHTSA adds models.</summary>
    public TimeSpan ModelsDuration { get; init; } = TimeSpan.FromHours(12);

    /// <summary>An empty answer may be filled in soon (for example a new model year), so it is kept briefly.</summary>
    public TimeSpan EmptyResultDuration { get; init; } = TimeSpan.FromMinutes(10);
}
