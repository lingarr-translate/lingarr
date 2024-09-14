using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Providers;

public interface ISonarrSettingsProvider
{
    /// <summary>
    /// Asynchronously retrieves the Sonarr settings, including the Sonarr URL and API key.
    /// </summary>
    /// <returns>
    /// The task result contains a <see cref="SonarrSettings"/> object 
    /// with the Sonarr URL and API key, or <c>null</c> if the settings could not be retrieved.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if Sonarr settings are not configured correctly or are missing.</exception>
    Task<SonarrSettings?> GetSonarrSettings();
}