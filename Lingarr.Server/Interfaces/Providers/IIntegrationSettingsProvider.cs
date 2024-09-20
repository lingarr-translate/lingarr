using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Providers;

public interface IIntegrationSettingsProvider
{
    /// <summary>
    /// Retrieves the integration settings based on the provided setting keys.
    /// </summary>
    /// <param name="settingKeys">The keys used to identify and fetch the required settings.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// - An <see cref="IntegrationSettings"/> object if the settings are successfully retrieved and all required values are present.
    /// - Null if any required setting is missing or if there's an error in retrieving the settings.
    /// </returns>
    Task<IntegrationSettings?> GetSettings(IntegrationSettingKeys settingKeys);
}