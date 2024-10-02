namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for managing application settings.
/// </summary>
public interface ISettingService
{
    /// <summary>
    /// Asynchronously retrieves the value of a setting by its key.
    /// </summary>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the setting value, or <c>null</c> if the key is not found.</returns>
    Task<string?> GetSetting(string key);

    /// <summary>
    /// Asynchronously retrieves the values of multiple settings by their keys.
    /// </summary>
    /// <param name="keys">An enumerable collection of keys for the settings to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary where the keys are the setting keys and the values are the corresponding setting values.</returns>
    Task<Dictionary<string, string>> GetSettings(IEnumerable<string> keys);

    /// <summary>
    /// Asynchronously updates the value of a setting.
    /// </summary>
    /// <param name="key">The key of the setting to update.</param>
    /// <param name="value">The new value to set.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the update was successful, <c>false</c> otherwise.</returns>
    Task<bool> SetSetting(string key, string value);

    /// <summary>
    /// Asynchronously updates multiple settings with the specified keys and values.
    /// </summary>
    /// <param name="settings">A dictionary where the keys are setting keys and the values are the new values to assign.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if all settings were successfully updated, <c>false</c> otherwise.</returns>
    Task<bool> SetSettings(Dictionary<string, string> settings);
    
    
    Task<List<T>> GetSettingAsJson<T>(string key) where T : class;
}