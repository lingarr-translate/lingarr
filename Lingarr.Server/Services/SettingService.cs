using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class SettingService: ISettingService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<SettingService> _logger;

    public SettingService(
        IBackgroundJobClient backgroundJobClient,
        LingarrDbContext dbContext, 
        ILogger<SettingService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _dbContext = dbContext;
        _logger = logger;
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
        await HandleModifiedSettingsAsync(settings);
        return true;
    }

    /// <inheritdoc />
    public async Task HandleModifiedSettingsAsync(Dictionary<string, string> modifiedKeys)
    {
        string[] requiredRadarrKeys = { "radarr_api_key", "radarr_url" };
        string[] requiredSonarrKeys = { "sonarr_api_key", "sonarr_url" };
        
        if (requiredRadarrKeys.Any(key => modifiedKeys.ContainsKey(key)))
        {
            await CheckAndRunJob("Radarr", requiredRadarrKeys);
        }

        if (requiredSonarrKeys.Any(key => modifiedKeys.ContainsKey(key)))
        {
           await CheckAndRunJob("Sonarr", requiredSonarrKeys);
        }
    }
    
    /// <summary>
    /// This method retrieves the required settings from the database. If all required settings have non-empty values,
    /// it enqueues the appropriate background job based on the <paramref name="jobName"/>:
    /// /// </summary>
    /// <param name="jobName">The name of the job to run, either "Radarr" or "Sonarr".</param>
    /// <param name="requiredKeys">An array of setting keys that must have values in the database.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CheckAndRunJob(string jobName, string[] requiredKeys)
    {
        var settings = await _dbContext.Settings
            .Where(s => requiredKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        
        bool allRequiredKeysHaveValues = requiredKeys.All(key => 
            settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value));

        if (allRequiredKeysHaveValues)
        {
            switch (jobName)
            {
                case "Radarr":
                    _logger.LogInformation("Radarr settings completed, indexing media...");
                    _backgroundJobClient.Enqueue<GetMovieJob>(x => x.Execute());
                    break;
                case "Sonarr":
                    _logger.LogInformation("Sonarr settings completed, indexing media...");
                    _backgroundJobClient.Enqueue<GetShowJob>(x => x.Execute());
                    break;
            }
        }
    }
}