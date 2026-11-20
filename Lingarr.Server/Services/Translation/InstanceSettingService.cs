using System.Text.Json;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services.Translation;

/// <summary>
/// Reads the settings of one translation service instance and falls back to the global settings for every other key.
/// Writes go to the global settings unchanged.
/// </summary>
public class InstanceSettingService : ISettingService
{
    private readonly ISettingService _settingService;
    private readonly LingarrDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly int _translationServiceInstanceId;
    private readonly IReadOnlySet<string> _instanceSettingKeys;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Dictionary<string, string>? _instanceSettings;

    public InstanceSettingService(
        ISettingService settingService,
        LingarrDbContext dbContext,
        IEncryptionService encryptionService,
        int translationServiceInstanceId,
        IReadOnlySet<string> instanceSettingKeys)
    {
        _settingService = settingService;
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _translationServiceInstanceId = translationServiceInstanceId;
        _instanceSettingKeys = instanceSettingKeys;
    }

    public event SettingChangedHandler SettingChanged
    {
        add => _settingService.SettingChanged += value;
        remove => _settingService.SettingChanged -= value;
    }

    /// <inheritdoc />
    public async Task<string?> GetSetting(string key)
    {
        var instanceSettings = await LoadInstanceSettings();
        if (instanceSettings.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_instanceSettingKeys.Contains(key))
        {
            return string.Empty;
        }

        return await _settingService.GetSetting(key);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetSettings(IEnumerable<string> keys)
    {
        var instanceSettings = await LoadInstanceSettings();
        var result = new Dictionary<string, string>();
        var globalKeys = new List<string>();

        foreach (var key in keys)
        {
            if (instanceSettings.TryGetValue(key, out var value))
            {
                result[key] = value;
            }
            else if (_instanceSettingKeys.Contains(key))
            {
                result[key] = string.Empty;
            }
            else
            {
                globalKeys.Add(key);
            }
        }

        if (globalKeys.Count == 0)
        {
            return result;
        }

        var globalSettings = await _settingService.GetSettings(globalKeys);
        foreach (var setting in globalSettings)
        {
            result[setting.Key] = setting.Value;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<T>> GetSettingAsJson<T>(string key) where T : class
    {
        var instanceSettings = await LoadInstanceSettings();
        if (!instanceSettings.TryGetValue(key, out var value))
        {
            return await _settingService.GetSettingAsJson<T>(key);
        }

        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(value) ?? [];
    }

    /// <inheritdoc />
    public Task<bool> SetSetting(string key, string value)
    {
        return _settingService.SetSetting(key, value);
    }

    /// <inheritdoc />
    public Task<bool> SetSettings(Dictionary<string, string> settings)
    {
        return _settingService.SetSettings(settings);
    }

    /// <inheritdoc />
    public Task<bool> SetEncryptedSetting(string key, string value)
    {
        return _settingService.SetEncryptedSetting(key, value);
    }

    /// <inheritdoc />
    public async Task<string?> GetEncryptedSetting(string key)
    {
        var value = await GetSetting(key);
        if (!string.IsNullOrEmpty(value))
        {
            return _encryptionService.Decrypt(value);
        }
        return value;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetEncryptedSettings(IEnumerable<string> keys)
    {
        var settings = await GetSettings(keys);

        var decrypted = new Dictionary<string, string>();
        foreach (var setting in settings)
        {
            decrypted[setting.Key] = _encryptionService.Decrypt(setting.Value);
        }

        return decrypted;
    }

    private async Task<Dictionary<string, string>> LoadInstanceSettings()
    {
        if (_instanceSettings is not null)
        {
            return _instanceSettings;
        }

        await _loadLock.WaitAsync();
        try
        {
            _instanceSettings ??= await _dbContext.TranslationServiceInstanceSettings
                .AsNoTracking()
                .Where(setting => setting.TranslationServiceInstanceId == _translationServiceInstanceId)
                .ToDictionaryAsync(setting => setting.Key, setting => setting.Value);
            return _instanceSettings;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
