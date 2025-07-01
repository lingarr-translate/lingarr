using Hangfire;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Lingarr.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Listener;

public class SettingChangedListener
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<SettingUpdatesHub> _hubContext;
    private readonly ILogger<SettingChangedListener> _logger;
    private static readonly HashSet<string> BatchServiceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai", "anthropic", "localai", "gemini"
    };

    public SettingChangedListener(IServiceProvider serviceProvider,
        IHubContext<SettingUpdatesHub> hubContext,
        ILogger<SettingChangedListener> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async void OnSettingChanged(SettingService settingService, string setting)
    {
        var settingGroups = new Dictionary<string, (string actionType, string actionName, string[] keys)>
        {
            {
                "radarr", ("Job", "Radarr", [
                    SettingKeys.Integration.RadarrApiKey,
                    SettingKeys.Integration.RadarrUrl
                ])
            },
            {
                "sonarr", ("Job", "Sonarr", [
                    SettingKeys.Integration.SonarrApiKey,
                    SettingKeys.Integration.SonarrUrl
                ])
            },
            {
                "automation", ("Job", "Automation", [
                    SettingKeys.Automation.AutomationEnabled,
                    SettingKeys.Automation.TranslationSchedule,
                    SettingKeys.Automation.MaxTranslationsPerRun
                ])
            },
            {
                "clearHash", ("Action", "ClearHash", [
                    SettingKeys.Translation.SourceLanguages
                ])
            },
            {
                "schedule", ("Action", "Schedule", [
                    SettingKeys.Automation.MovieSchedule,
                    SettingKeys.Automation.ShowSchedule
                ])
            },
            {
                "serviceType", ("Action", "ServiceType", [
                    SettingKeys.Translation.ServiceType
                ])
            },
            {
                "batchTranslation", ("Action", "BatchTranslation", [
                    SettingKeys.Translation.UseBatchTranslation
                ])
            }
        };

        // Find and execute the appropriate action for the changed setting
        foreach (var group in settingGroups)
        {
            // Check if the changed setting belongs to this configuration group based on it's *keys*
            if (group.Value.keys.Contains(setting))
            {
                switch (group.Value.actionType)
                {
                    case "Job":
                        await RunJob(group.Value.actionName, group.Value.keys);
                        break;
                    case "Action":
                        await RunAction(group.Value.actionName, group.Value.keys);
                        break;
                }

                break;
            }
        }
    }

    /// <summary>
    /// This method retrieves the required settings from the database. If all required settings have non-empty values,
    /// it enqueues the appropriate background job based on the <paramref name="jobName"/>:
    /// </summary>
    /// <param name="jobName">The name of the job to run.</param>
    /// <param name="requiredKeys">An array of setting keys that must have values in the database.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunJob(string jobName, string[] requiredKeys)
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
                    _logger.LogInformation(
                        $"Settings changed for |Green|{jobName}|/Green|. All settings are complete, |Orange|indexing media...|/Orange|");

                    await _hubContext.Clients.Group("SettingUpdates").SendAsync("SettingUpdate", new
                    {
                        Key = SettingKeys.Integration.RadarrSettingsCompleted,
                        Value = "true"
                    });

                    await settingService.SetSetting(SettingKeys.Integration.RadarrSettingsCompleted, "true");
                    BackgroundJob.Schedule<SyncMovieJob>(job => job.Execute(), TimeSpan.FromMinutes(1));
                    break;
                case "Sonarr":
                    _logger.LogInformation(
                        $"Settings changed for |Green|{jobName}|/Green|. All settings are complete, |Orange|indexing media...|/Orange|");

                    await _hubContext.Clients.Group("SettingUpdates").SendAsync("SettingUpdate", new
                    {
                        Key = SettingKeys.Integration.SonarrSettingsCompleted,
                        Value = "true"
                    });

                    await settingService.SetSetting(SettingKeys.Integration.SonarrSettingsCompleted, "true");
                    BackgroundJob.Schedule<SyncShowJob>(job => job.Execute(), TimeSpan.FromMinutes(1));
                    break;
                case "Automation":
                    _logger.LogInformation(
                        $"Settings changed for |Green|{jobName}|/Green|. Automation has been |Orange|modified|/Orange|.");
                    if (settings[SettingKeys.Automation.AutomationEnabled] == "true")
                    {
                        var translationSchedule =
                            await settingService.GetSetting(SettingKeys.Automation.TranslationSchedule);
                        RecurringJob.RemoveIfExists(SettingKeys.Automation.TranslationSchedule);
                        RecurringJob.AddOrUpdate<AutomatedTranslationJob>(
                            "AutomatedTranslationJob",
                            job => job.Execute(),
                            translationSchedule);
                    }
                    else
                    {
                        RecurringJob.RemoveIfExists("AutomatedTranslationJob");
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// This method retrieves the required settings from the database. If all required settings have non-empty values,
    /// it performs an action based on the <paramref name="actionName"/>:
    /// </summary>
    /// <param name="actionName">The name of the action to run.</param>
    /// <param name="requiredKeys">An array of setting keys that must have values in the database.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunAction(string actionName, string[] requiredKeys)
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
            switch (actionName)
            {
                case "ClearHash":
                    dbContext.Database.ExecuteSqlRaw("UPDATE movies SET media_hash = ''");
                    dbContext.Database.ExecuteSqlRaw("UPDATE episodes SET media_hash = ''");
                    break;

                case "Schedule":
                    RecurringJob.AddOrUpdate<SyncMovieJob>(
                        "SyncMovieJob",
                        job => job.Execute(),
                        settings[SettingKeys.Automation.MovieSchedule]);
                    RecurringJob.AddOrUpdate<SyncShowJob>(
                        "SyncShowJob",
                        job => job.Execute(),
                        settings[SettingKeys.Automation.ShowSchedule]);
                    break;

                case "ServiceType":
                    var serviceType = await settingService.GetSetting(SettingKeys.Translation.ServiceType);
                    if (serviceType != null && BatchServiceTypes.Contains(serviceType))
                    {
                        await settingService.SetSetting(SettingKeys.Translation.UseBatchTranslation, "false");
                    }
                    break;

                case "BatchTranslation":
                    var useBatchTranslation = await settingService.GetSetting(SettingKeys.Translation.UseBatchTranslation);
                    if (useBatchTranslation is "true")
                    {
                        await settingService.SetSetting(SettingKeys.Translation.AiContextPromptEnabled, "false");
                    }
                    break;
            }
        }
    }
}