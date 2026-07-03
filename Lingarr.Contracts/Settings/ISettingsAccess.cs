using Lingarr.Contracts.Translation;

namespace Lingarr.Contracts.Settings;

/// <summary>
/// Reads the host's settings store for the configuration keys a plugin declares in its
/// manifest. Plugins cannot write settings; all changes are made through the settings UI.
/// </summary>
public interface ISettingsAccess
{
    Task<string?> GetSettingAsync(string key);

    Task<IReadOnlyDictionary<string, string>> GetSettingsAsync(IEnumerable<string> keys);

    Task<string?> GetEncryptedSettingAsync(string key);

    /// <summary>
    /// Returns the host's shared HTTP timeout and retry settings so a plugin can reuse the same
    /// backoff behaviour as the built-in providers.
    /// </summary>
    Task<TranslationHttpSettings> GetHttpSettingsAsync();
}
