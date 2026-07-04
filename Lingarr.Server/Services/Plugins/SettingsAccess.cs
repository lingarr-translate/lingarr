using Lingarr.Contracts.Settings;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Plugins;

internal sealed class SettingsAccess : ISettingsAccess
{
    private readonly ISettingService _settings;

    public SettingsAccess(ISettingService settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    public Task<string?> GetSettingAsync(string key)
    {
        return _settings.GetSetting(key);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetSettingsAsync(IEnumerable<string> keys)
    {
        var values = await _settings.GetSettings(keys);
        return values;
    }

    /// <inheritdoc />
    public Task<string?> GetEncryptedSettingAsync(string key)
    {
        return _settings.GetEncryptedSetting(key);
    }

    /// <inheritdoc />
    public async Task<TranslationHttpSettings> GetHttpSettingsAsync()
    {
        var settings = await _settings.GetSettings([
            SettingKeys.Translation.RequestTimeout,
            SettingKeys.Translation.MaxRetries,
            SettingKeys.Translation.RetryDelay,
            SettingKeys.Translation.RetryDelayMultiplier
        ]);

        var timeoutMinutes = int.TryParse(settings[SettingKeys.Translation.RequestTimeout], out var timeout) && timeout > 0
            ? timeout
            : 5;

        var maxRetries = int.TryParse(settings[SettingKeys.Translation.MaxRetries], out var retries)
            ? retries
            : 3;

        var retryDelaySeconds = int.TryParse(settings[SettingKeys.Translation.RetryDelay], out var delaySeconds)
            ? delaySeconds
            : 2;

        var retryDelayMultiplier = int.TryParse(settings[SettingKeys.Translation.RetryDelayMultiplier], out var multiplier)
            ? multiplier
            : 2;

        return new TranslationHttpSettings(
            TimeSpan.FromMinutes(timeoutMinutes),
            maxRetries,
            TimeSpan.FromSeconds(retryDelaySeconds),
            retryDelayMultiplier);
    }
}
