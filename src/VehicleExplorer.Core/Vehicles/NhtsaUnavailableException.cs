namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// NHTSA could not answer: it is down, too slow, throttling us, or sent something that is not valid JSON.
/// The API turns it into 503 Service Unavailable.
/// </summary>
public sealed class NhtsaUnavailableException(string message, Exception innerException) : Exception(message, innerException);
