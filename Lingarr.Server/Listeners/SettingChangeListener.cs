using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Listeners;

public class SettingChangeListener : ISettingChangeHandler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<ISettingService> _logger;
    private readonly ISettingService _settingService;

    public SettingChangeListener(
        IBackgroundJobClient backgroundJobClient,
        LingarrDbContext dbContext, 
        ISettingService settingService,
        ILogger<ISettingService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _settingService = settingService;
        _dbContext = dbContext;
        _logger = logger;
    }
    public async void HandleSettingChange(string key, string value)
    {
        string[] requiredRadarrKeys = { "radarr_api_key", "radarr_url" };
        string[] requiredSonarrKeys = { "sonarr_api_key", "sonarr_url" };
        string[] requiredAutomationKeys = { "automation_enabled" };
        
        if (requiredRadarrKeys.Any(requiredKey => requiredKey == key))
        {
            await CheckAndRunJob("Radarr", requiredRadarrKeys);
        }

        if (requiredSonarrKeys.Any(requiredKey => requiredKey == key))
        {
            await CheckAndRunJob("Sonarr", requiredSonarrKeys);
        }

        if (requiredAutomationKeys.Any(requiredKey => requiredKey == key))
        {
            await CheckAndRunJob("Automation", requiredAutomationKeys);
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
                    await _settingService.SetSetting("radarr_settings_completed", "true");
                    
                    _logger.LogInformation("Radarr settings completed, indexing media...");
                    _backgroundJobClient.Schedule<GetMovieJob>("movies",
                        job => job.Execute(JobCancellationToken.Null),
                        TimeSpan.FromMinutes(1));
                    break;
                case "Sonarr":
                    await _settingService.SetSetting("sonarr_settings_completed", "true");
                    
                    _logger.LogInformation("Sonarr settings completed, indexing media...");
                    _backgroundJobClient.Schedule<GetShowJob>("shows",
                        job => job.Execute(JobCancellationToken.Null),
                        TimeSpan.FromMinutes(1));
                    break;
                case "Automation":
                    var translationSchedule = await _settingService.GetSetting("translation_schedule");
                    RecurringJob.AddOrUpdate<AutomatedTranslationJob>(
                        "AutomatedTranslationJob",
                        "default",
                        job => job.Execute(),
                        translationSchedule);
                    break;
            }
        }
    }
}