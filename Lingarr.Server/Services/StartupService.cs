using System.Data.Common;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class StartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupService> _logger;

    public StartupService(IServiceProvider serviceProvider, ILogger<StartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the application by validating and updating integration settings for integration services.
    /// This method is part of the application startup process and ensures all required service
    /// configurations are properly set.
    /// </summary>
    /// <param name="cancellationToken">Allows for cancellation of the startup process.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LingarrDbContext>();

        await EnsureDefaultSettings(dbContext);
        await EnsureStatisticsSchema(dbContext);
        await ApplySettingsFromEnvironment(dbContext);

        await CheckAndUpdateIntegrationSettings(dbContext, "radarr", [
            SettingKeys.Integration.RadarrUrl,
            SettingKeys.Integration.RadarrApiKey
        ]);

        await CheckAndUpdateIntegrationSettings(dbContext, "sonarr", [
            SettingKeys.Integration.SonarrUrl,
            SettingKeys.Integration.SonarrApiKey
        ]);
    }

    private async Task EnsureStatisticsSchema(LingarrDbContext dbContext)
    {
        try
        {
            var provider = dbContext.Database.ProviderName ?? string.Empty;
            var columnMissing = await IsStatisticsModelColumnMissing(dbContext);

            if (!columnMissing)
            {
                return;
            }

            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE statistics ADD COLUMN translations_by_model_json TEXT NOT NULL DEFAULT '{}'" );
                _logger.LogInformation("Added translations_by_model_json column to statistics (SQLite self-heal).");
            }
            else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE statistics ADD COLUMN translations_by_model_json LONGTEXT NOT NULL DEFAULT '{}'" );
                _logger.LogInformation("Added translations_by_model_json column to statistics (MySQL self-heal).");
            }
            else
            {
                _logger.LogWarning("Unable to self-heal statistics schema for provider {Provider}", provider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure statistics schema");
            throw;
        }
    }

    private static async Task<bool> IsStatisticsModelColumnMissing(LingarrDbContext dbContext)
    {
        await using DbConnection connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        if (dbContext.Database.IsSqlite())
        {
            command.CommandText = "SELECT 1 FROM pragma_table_info('statistics') WHERE name = 'translations_by_model_json'";
        }
        else
        {
            command.CommandText = "SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'statistics' AND COLUMN_NAME = 'translations_by_model_json'";
        }

        var result = await command.ExecuteScalarAsync();
        return result == null;
    }

    private static async Task EnsureDefaultSettings(LingarrDbContext dbContext)
    {
        // Seed critical onboarding/auth/telemetry defaults if they were missing due to skipped migrations
        var requiredDefaults = new Dictionary<string, string>
        {
            { SettingKeys.Authentication.OnboardingCompleted, "false" },
            { SettingKeys.Authentication.AuthEnabled, "false" },
            { SettingKeys.Telemetry.TelemetryEnabled, "false" },
            { SettingKeys.Telemetry.LastSubmission, string.Empty },
            { SettingKeys.Telemetry.LastReportedLines, "0" },
            { SettingKeys.Telemetry.LastReportedFiles, "0" },
            { SettingKeys.Telemetry.LastReportedCharacters, "0" }
        };

        foreach (var kvp in requiredDefaults)
        {
            var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
            if (setting == null)
            {
                dbContext.Settings.Add(new Setting { Key = kvp.Key, Value = kvp.Value });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Validates and updates completion status for integration settings of a specific service.
    /// </summary>
    /// <param name="dbContext">The database context for accessing settings.</param>
    /// <param name="serviceName">Name of the service being validated (e.g., "radarr", "sonarr").</param>
    /// <param name="requiredKeys">Array of setting keys that must be present and non-empty for the service.</param>
    private async Task CheckAndUpdateIntegrationSettings(LingarrDbContext dbContext, string serviceName, string[] requiredKeys)
    {
        var completedKey = serviceName == "radarr"
            ? SettingKeys.Integration.RadarrSettingsCompleted
            : SettingKeys.Integration.SonarrSettingsCompleted;

        var settings = await dbContext.Settings
            .Where(s => requiredKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var allRequiredKeysHaveValues = requiredKeys.All(key =>
            settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value));

        if (allRequiredKeysHaveValues)
        {
            var setting = await dbContext.Settings.FindAsync(completedKey);
            if (setting != null)
            {
                setting.Value = "true";
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"{serviceName} settings completed.");
            }
        }
    }

    /// <summary>
    /// Applies settings from environment variables to the database.
    /// </summary>
    /// <param name="dbContext">The database context used to access and update settings.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ApplySettingsFromEnvironment(LingarrDbContext dbContext)
    {
        var environmentSettings = new Dictionary<string, string>
        {
            { "RADARR_URL", SettingKeys.Integration.RadarrUrl },
            { "RADARR_API_KEY", SettingKeys.Integration.RadarrApiKey },
            { "SONARR_URL", SettingKeys.Integration.SonarrUrl },
            { "SONARR_API_KEY", SettingKeys.Integration.SonarrApiKey },
            { "SOURCE_LANGUAGES", SettingKeys.Translation.SourceLanguages },
            { "TARGET_LANGUAGES", SettingKeys.Translation.TargetLanguages },

            { "SERVICE_TYPE", SettingKeys.Translation.ServiceType },
            { "LIBRE_TRANSLATE_URL", SettingKeys.Translation.LibreTranslate.Url },
            { "LIBRE_TRANSLATE_API_KEY", SettingKeys.Translation.LibreTranslate.ApiKey },
            { "AI_PROMPT", SettingKeys.Translation.AiPrompt },

            { "OPENAI_MODEL", SettingKeys.Translation.OpenAi.Model },
            { "OPENAI_API_KEY", SettingKeys.Translation.OpenAi.ApiKey },

            { "ANTHROPIC_MODEL", SettingKeys.Translation.Anthropic.Model },
            { "ANTHROPIC_API_KEY", SettingKeys.Translation.Anthropic.ApiKey },
            { "ANTHROPIC_VERSION", SettingKeys.Translation.Anthropic.Version },

            { "LOCAL_AI_MODEL", SettingKeys.Translation.LocalAi.Model },
            { "LOCAL_AI_API_KEY", SettingKeys.Translation.LocalAi.ApiKey },
            { "LOCAL_AI_ENDPOINT", SettingKeys.Translation.LocalAi.Endpoint },

            { "GEMINI_MODEL", SettingKeys.Translation.Gemini.Model },
            { "GEMINI_API_KEY", SettingKeys.Translation.Gemini.ApiKey },

            { "DEEPSEEK_MODEL", SettingKeys.Translation.DeepSeek.Model },
            { "DEEPSEEK_API_KEY", SettingKeys.Translation.DeepSeek.ApiKey },

            { "DEEPL_API_KEY", SettingKeys.Translation.DeepL.DeeplApiKey },

            { "AUTH_ENABLED", SettingKeys.Authentication.AuthEnabled },
            { "TELEMETRY_ENABLED", SettingKeys.Telemetry.TelemetryEnabled }
        };

        foreach (var (envVar, settingKey) in environmentSettings)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            
            var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Key == settingKey);
            if (setting != null)
            {
                setting.Value = value;
                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation($"Updated setting '{settingKey}' from environment variable '{envVar}'.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
