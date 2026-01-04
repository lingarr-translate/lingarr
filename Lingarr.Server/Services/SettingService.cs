using System.Text.Json;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lingarr.Server.Services;

public delegate void SettingChangedHandler(SettingService ss, string setting);

public class SettingService : ISettingService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<ISettingService> _logger;
    public event SettingChangedHandler SettingChanged;
    
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public SettingService(
        LingarrDbContext dbContext,
        ILogger<ISettingService> logger,
        IMemoryCache memoryCache,
        SettingChangedListener settingChangedListener)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = memoryCache;
        
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(1))
            .SetSlidingExpiration(TimeSpan.FromMinutes(30));
        
        SettingChanged += settingChangedListener.OnSettingChanged;
        SettingChanged += InvalidateCacheForSetting;
    }

    private void InvalidateCacheForSetting(SettingService ss, string setting)
    {
        _cache.Remove(setting);
    }

    public void OnSettingChange(string setting)
    {
        SettingChanged?.Invoke(this, setting); 
    }

    /// <inheritdoc />
    public async Task<string?> GetSetting(string key)
    {
        if (_cache.TryGetValue(key, out string? cachedValue))
        {
            return cachedValue;
        }

        var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        var value = setting?.Value;
        
        if (value != null)
        {
            _cache.Set(key, value, _cacheOptions);
        }
        
        return value;
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetSettings(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, string>();
        var keysToFetch = new List<string>();

        foreach (var key in keys)
        {
            if (_cache.TryGetValue(key, out string? cachedValue))
            {
                if (cachedValue != null)
                {
                    result[key] = cachedValue;
                }
            }
            else
            {
                keysToFetch.Add(key);
            }
        }

        if (keysToFetch.Any())
        {
            var dbSettings = await _dbContext.Settings
                .Where(s => keysToFetch.Contains(s.Key))
                .ToListAsync();
            
            foreach (var setting in dbSettings)
            {
                result[setting.Key] = setting.Value;
                _cache.Set(setting.Key, setting.Value, _cacheOptions);
            }
        }
        
        return result;
    }

    public async Task<List<T>> GetSettingAsJson<T>(string key) where T : class
    {
        var settingValue = await GetSetting(key);
        if (string.IsNullOrEmpty(settingValue))
        {
            return []; 
        }

        try
        {
            var result = JsonSerializer.Deserialize<List<T>>(settingValue);

            return result ?? [];
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
            // Upsert missing setting rows so callers (like onboarding) never silently fail
            _dbContext.Settings.Add(new Setting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await _dbContext.SaveChangesAsync();
        OnSettingChange(key);
        return true;
    }
    
    /// <inheritdoc />
    public async Task<bool> SetSettings(Dictionary<string, string> settings)
    {
        var keys = settings.Keys.ToList();
        var existingSettings = await _dbContext.Settings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s);

        foreach (var setting in settings)
        {
            if (existingSettings.TryGetValue(setting.Key, out var existingSetting))
            {
                existingSetting.Value = setting.Value;
            }
            else
            {
                // Upsert any missing settings to avoid silent onboarding failures
                _dbContext.Settings.Add(new Setting { Key = setting.Key, Value = setting.Value });
            }
        }

        await _dbContext.SaveChangesAsync();
        foreach (var setting in settings)
        {
            OnSettingChange(setting.Key);
        }
        return true;
    }
}