namespace VehicleExplorer.Infrastructure.Nhtsa;

/// <summary>NHTSA settings. The default works out of the box; override with the "Nhtsa" configuration section.</summary>
public sealed class NhtsaOptions
{
    public const string SectionName = "Nhtsa";

    public Uri BaseAddress { get; init; } = new("https://vpic.nhtsa.dot.gov/api/vehicles/");
}
