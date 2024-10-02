using System.Text.Json;
using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Server.Events;
using Lingarr.Server.Interfaces;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class SettingService: ISettingService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<ISettingService> _logger;
    private readonly List<ISettingChangeHandler> _handlers = new List<ISettingChangeHandler>();
    public event EventHandler<SettingChangeEventArgs> SettingChanged;

    public SettingService(
        IBackgroundJobClient backgroundJobClient,
        LingarrDbContext dbContext, 
        ILogger<ISettingService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public void RegisterHandler(ISettingChangeHandler handler)
    {
        _handlers.Add(handler);
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
        // _logger.LogInformation("Retrieved the following settingValue `{settingValue}`", settingValue);
    
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
        NotifySettingChange(key, value);
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
            NotifySettingChange(setting.Key, setting.Value);
        }
        return true;
    }

    private void NotifySettingChange(string key, string value)
    {
        foreach (var handler in _handlers)
        {
            handler.HandleSettingChange(key, value);
        }
        SettingChanged.Invoke(this, new SettingChangeEventArgs(key, value));
    }
}