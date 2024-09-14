using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Providers;

public interface IRadarrSettingsProvider
{
    /// <summary>
    /// Asynchronously retrieves the Radarr settings, including the Radarr URL and API key.
    /// </summary>
    /// <returns>
    /// The task result contains a <see cref="RadarrSettings"/> object 
    /// with the Radarr URL and API key, or <c>null</c> if the settings could not be retrieved.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if Radarr settings are not configured correctly or are missing.</exception>
    Task<RadarrSettings?> GetRadarrSettings();
}