using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Listener;

public class SettingChangedListener
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingChangedListener> _logger;

    public SettingChangedListener(IServiceProvider serviceProvider, ILogger<SettingChangedListener> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async void OnSettingChanged(SettingService ss, string setting)
    {
        string[] requiredRadarrKeys = { "radarr_api_key", "radarr_url" };
        string[] requiredSonarrKeys = { "sonarr_api_key", "sonarr_url" };
        string[] requiredAutomationKeys = { "automation_enabled" };
        
        if (requiredRadarrKeys.Any(requiredKey => requiredKey == setting))
        {
            await CheckAndRunJob("Radarr", requiredRadarrKeys);
        }
        
        if (requiredSonarrKeys.Any(requiredKey => requiredKey == setting))
        {
            await CheckAndRunJob("Sonarr", requiredSonarrKeys);
        }
        
        if (requiredAutomationKeys.Any(requiredKey => requiredKey == setting))
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
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LingarrDbContext>();
        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
        
        var settings = await dbContext.Settings
            .Where(s => requiredKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        
        bool allRequiredKeysHaveValues = requiredKeys.All(key => 
            settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value));
    
        if (allRequiredKeysHaveValues)
        {
            switch (jobName)
            {
                case "Radarr":
                    await settingService.SetSetting("radarr_settings_completed", "true");
                    
                    _logger.LogInformation("Radarr settings completed, indexing media...");
                    BackgroundJob.Schedule<GetMovieJob>("movies",
                        job => job.Execute(JobCancellationToken.Null),
                        TimeSpan.FromMinutes(1));
                    break;
                case "Sonarr":
                    await settingService.SetSetting("sonarr_settings_completed", "true");
                    
                    _logger.LogInformation("Sonarr settings completed, indexing media...");
                    BackgroundJob.Schedule<GetShowJob>("shows",
                        job => job.Execute(JobCancellationToken.Null),
                        TimeSpan.FromMinutes(1));
                    break;
                case "Automation":
                    var translationSchedule = await settingService.GetSetting("translation_schedule");
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