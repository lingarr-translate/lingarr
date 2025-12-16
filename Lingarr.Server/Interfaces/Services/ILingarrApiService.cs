using Lingarr.Server.Models.Telemetry;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Service for communicating with the Lingarr API (api.lingarr.com)
/// </summary>
public interface ILingarrApiService
{
    /// <summary>
    /// Gets the latest recommended version from Lingarr API
    /// </summary>
    /// <returns>Version string or null if unavailable</returns>
    Task<string?> GetLatestVersion();

    /// <summary>
    /// Submits telemetry data to Lingarr API
    /// </summary>
    /// <param name="payload">Telemetry payload to submit</param>
    /// <returns>True if submission was successful, false otherwise</returns>
    Task<bool> SubmitTelemetry(TelemetryPayload payload);
}
