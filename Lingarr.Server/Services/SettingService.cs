using System.Text.Json;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Listener;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public delegate void SettingChangedHandler(SettingService ss, string setting);

public class SettingService : ISettingService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<ISettingService> _logger;
    public event SettingChangedHandler SettingChanged;

    public SettingService(
        LingarrDbContext dbContext,
        ILogger<ISettingService> logger,
        SettingChangedListener settingChangedListener)
    {
        _dbContext = dbContext;
        _logger = logger;
        
        SettingChanged += settingChangedListener.OnSettingChanged;
    }

    public void OnSettingChange(string setting)
    {
        SettingChanged?.Invoke(this, setting); 
    }

    /// <inheritdoc />
    public async Task<string?> GetSetting(string key)
    {
        var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetSettings(IEnumerable<string> keys)
    {
        var settings = await _dbContext.Settings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync();
        
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task<List<T>> GetSettingAsJson<T>(string key) where T : class
    {
        var settingValue = await GetSetting(key);
        if (string.IsNullOrEmpty(settingValue))
        {
            return new List<T>(); 
        }

        try
        {
            var result = JsonSerializer.Deserialize<List<T>>(settingValue);

            return result ?? new List<T>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize setting '{Key}'. Value: {Value}", key, settingValue);
            throw new JsonException($"Failed to deserialize setting '{key}'", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> SetSetting(string key, string value)
    {
        var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            return false;
        }

        setting.Value = value;
        await _dbContext.SaveChangesAsync();
        OnSettingChange(setting.Key);
        return true;
    }
    
    /// <inheritdoc />
    public async Task<bool> SetSettings(Dictionary<string, string> settings)
    {
        var keys = settings.Keys.ToList();
        var existingSettings = await _dbContext.Settings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s);

        if (existingSettings.Count != keys.Count)
        {
            // Not all settings were found
            return false;
        }

        foreach (var setting in settings)
        {
            var existingSetting = existingSettings[setting.Key];
            existingSetting.Value = setting.Value;
        }

        await _dbContext.SaveChangesAsync();
        foreach (var setting in settings)
        {
            OnSettingChange(setting.Key);
        }
        return true;
    }
}