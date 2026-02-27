using Lingarr.Server.Services;
using Newtonsoft.Json;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for managing application settings.
/// </summary>
public interface ISettingService
{
    /// <summary>
    /// Occurs when a setting is changed.
    /// </summary>
    event SettingChangedHandler SettingChanged;
    
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
    
    /// <summary>
    /// Retrieves a setting value as a JSON-deserialized list of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects in the list. Must be a reference type.</typeparam>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of objects of type T deserialized from the JSON setting value.</returns>
    /// <remarks>
    /// This method assumes that the setting value is stored as a JSON array string that can be deserialized into a list of T objects.
    /// </remarks>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to List{T}.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the specified key does not exist in the settings.</exception>
    Task<List<T>> GetSettingAsJson<T>(string key) where T : class;

    /// <summary>
    /// Encrypts and stores a single setting value.
    /// </summary>
    /// <param name="key">The key of the setting to store.</param>
    /// <param name="value">The plaintext value to encrypt and store.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the update was successful, <c>false</c> otherwise.</returns>
    Task<bool> SetEncryptedSetting(string key, string value);

    /// <summary>
    /// Retrieves and decrypts a single setting value.
    /// </summary>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>The decrypted plaintext value, or <c>null</c> if the key is not found or the value cannot be decrypted.</returns>
    Task<string?> GetEncryptedSetting(string key);

    /// <summary>
    /// Retrieves and decrypts multiple setting values.
    /// </summary>
    /// <param name="keys">An enumerable collection of keys for the settings to retrieve.</param>
    /// <returns>A dictionary of decrypted values. Keys whose values cannot be decrypted are returned as empty string.</returns>
    Task<Dictionary<string, string>> GetEncryptedSettings(IEnumerable<string> keys);
}