using System.Text.RegularExpressions;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services.Translation;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class StartupService : IHostedService
{
    private const string ServiceTypeEnvironmentVariable = "SERVICE_TYPE";

    private static readonly (string EnvironmentVariable, string Provider, string SettingKey)[] TranslationServiceEnvironmentSettings =
    [
        ("LIBRE_TRANSLATE_URL", "libretranslate", SettingKeys.Translation.LibreTranslate.Url),
        ("LIBRE_TRANSLATE_API_KEY", "libretranslate", SettingKeys.Translation.LibreTranslate.ApiKey),

        ("OPENAI_MODEL", "openai", SettingKeys.Translation.OpenAi.Model),
        ("OPENAI_API_KEY", "openai", SettingKeys.Translation.OpenAi.ApiKey),

        ("ANTHROPIC_MODEL", "anthropic", SettingKeys.Translation.Anthropic.Model),
        ("ANTHROPIC_API_KEY", "anthropic", SettingKeys.Translation.Anthropic.ApiKey),
        ("ANTHROPIC_VERSION", "anthropic", SettingKeys.Translation.Anthropic.Version),

        ("LOCAL_AI_MODEL", "localai", SettingKeys.Translation.LocalAi.Model),
        ("LOCAL_AI_API_KEY", "localai", SettingKeys.Translation.LocalAi.ApiKey),
        ("LOCAL_AI_ENDPOINT", "localai", SettingKeys.Translation.LocalAi.Endpoint),
        ("LOCAL_AI_STRUCTURED_OUTPUT", "localai", SettingKeys.Translation.LocalAi.StructuredOutput),

        ("GEMINI_MODEL", "gemini", SettingKeys.Translation.Gemini.Model),
        ("GEMINI_API_KEY", "gemini", SettingKeys.Translation.Gemini.ApiKey),

        ("DEEPSEEK_MODEL", "deepseek", SettingKeys.Translation.DeepSeek.Model),
        ("DEEPSEEK_API_KEY", "deepseek", SettingKeys.Translation.DeepSeek.ApiKey),

        ("MISTRAL_MODEL", "mistral", SettingKeys.Translation.Mistral.Model),
        ("MISTRAL_API_KEY", "mistral", SettingKeys.Translation.Mistral.ApiKey),

        ("XAI_MODEL", "xai", SettingKeys.Translation.XAi.Model),
        ("XAI_API_KEY", "xai", SettingKeys.Translation.XAi.ApiKey),

        ("DEEPL_API_KEY", "deepl", SettingKeys.Translation.DeepL.DeeplApiKey)
    ];

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

        await ApplySettingsFromEnvironment(dbContext);
        await ApplyTranslationServicesFromEnvironment(
            scope.ServiceProvider.GetRequiredService<ITranslationServiceInstanceService>());

        await CheckAndUpdateIntegrationSettings(dbContext, "radarr", [
            SettingKeys.Integration.RadarrUrl,
            SettingKeys.Integration.RadarrApiKey
        ]);

        await CheckAndUpdateIntegrationSettings(dbContext, "sonarr", [
            SettingKeys.Integration.SonarrUrl,
            SettingKeys.Integration.SonarrApiKey
        ]);
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

            { "USE_BATCH_TRANSLATION", SettingKeys.Translation.UseBatchTranslation },
            { "AI_PROMPT", SettingKeys.Translation.AiPrompt },
            { "AI_USER_PROMPT", SettingKeys.Translation.AiUserPrompt },
            { "PROOFREAD_PROMPT", SettingKeys.Translation.ProofreadPrompt },
            { "PROOFREAD_USER_PROMPT", SettingKeys.Translation.ProofreadUserPrompt },

            { "AUTH_ENABLED", SettingKeys.Authentication.AuthEnabled },
            { "TELEMETRY_ENABLED", SettingKeys.Telemetry.TelemetryEnabled }
        };

        foreach (var (envVar, settingKey) in environmentSettings)
        {
            var value = ReadEnvironmentValue(envVar);
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

    /// <summary>
    /// Builds the translation service cascade from SERVICE_TYPE and applies the provider environment variables to
    /// their instances. A variable with the suffix _N targets the N-th instance of its provider, ordered by id.
    /// </summary>
    private async Task ApplyTranslationServicesFromEnvironment(
        ITranslationServiceInstanceService translationServiceInstanceService)
    {
        var serviceType = ReadEnvironmentValue(ServiceTypeEnvironmentVariable);
        var cascadeFromEnvironment = !string.IsNullOrEmpty(serviceType);
        if (cascadeFromEnvironment)
        {
            await ApplyServiceTypeFromEnvironment(translationServiceInstanceService, serviceType!);
        }

        await ApplyProviderSettingsFromEnvironment(translationServiceInstanceService, cascadeFromEnvironment);
    }

    private async Task ApplyServiceTypeFromEnvironment(
        ITranslationServiceInstanceService translationServiceInstanceService,
        string serviceType)
    {
        if (!TranslationServices.TryParse(serviceType, out var parsedProviders))
        {
            _logger.LogWarning(
                "{EnvironmentVariable} is not a provider or a JSON array of providers, the configured translation services are kept.",
                ServiceTypeEnvironmentVariable);
            return;
        }

        var providers = parsedProviders
            .Select(provider => provider.Trim().ToLowerInvariant())
            .Where(provider => provider.Length > 0)
            .ToList();

        var instances = await translationServiceInstanceService.GetInstances();
        var instancesByProvider = instances
            .GroupBy(instance => instance.Provider.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.OrderBy(instance => instance.Id).ToList());
        var occurrences = new Dictionary<string, int>();
        var orderedIds = new List<int>();

        foreach (var provider in providers)
        {
            var occurrence = occurrences.GetValueOrDefault(provider);
            occurrences[provider] = occurrence + 1;

            if (instancesByProvider.TryGetValue(provider, out var providerInstances)
                && occurrence < providerInstances.Count)
            {
                orderedIds.Add(providerInstances[occurrence].Id);
                continue;
            }

            var created = await CreateInstanceFromEnvironment(
                translationServiceInstanceService,
                provider,
                ServiceTypeEnvironmentVariable);
            if (created is not null)
            {
                orderedIds.Add(created.Id);
            }
        }

        if (orderedIds.Count == 0)
        {
            _logger.LogWarning(
                "{EnvironmentVariable} lists no usable translation service, the configured translation services are kept.",
                ServiceTypeEnvironmentVariable);
            return;
        }

        foreach (var instance in instances.Where(instance => !orderedIds.Contains(instance.Id)))
        {
            await translationServiceInstanceService.DeleteInstance(instance.Id);
            _logger.LogWarning(
                "Deleted translation service {Name} for provider {Provider} because {EnvironmentVariable} does not list it.",
                instance.Name,
                instance.Provider,
                ServiceTypeEnvironmentVariable);
        }

        await translationServiceInstanceService.ReorderInstances(orderedIds);
    }

    private async Task ApplyProviderSettingsFromEnvironment(
        ITranslationServiceInstanceService translationServiceInstanceService,
        bool cascadeFromEnvironment)
    {
        var environmentVariableNames = Environment.GetEnvironmentVariables().Keys
            .Cast<string>()
            .ToList();

        foreach (var (environmentVariable, provider, settingKey) in TranslationServiceEnvironmentSettings)
        {
            foreach (var instanceNumber in GetInstanceNumbers(environmentVariable, environmentVariableNames))
            {
                var numberedVariable = instanceNumber == 1
                    ? environmentVariable
                    : $"{environmentVariable}_{instanceNumber}";
                var value = ReadEnvironmentValue(numberedVariable);
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                var instance = await GetProviderInstanceForEnvironment(
                    translationServiceInstanceService,
                    provider,
                    instanceNumber,
                    cascadeFromEnvironment,
                    numberedVariable);
                if (instance is null)
                {
                    continue;
                }

                if (!await translationServiceInstanceService.SetInstanceSetting(instance.Id, settingKey, value))
                {
                    _logger.LogWarning(
                        "Could not set {SettingKey} of translation service {Name} from environment variable {EnvironmentVariable}.",
                        settingKey,
                        instance.Name,
                        numberedVariable);
                    continue;
                }

                _logger.LogInformation(
                    "Updated setting {SettingKey} of translation service {Name} from environment variable {EnvironmentVariable}.",
                    settingKey,
                    instance.Name,
                    numberedVariable);
            }
        }
    }

    private async Task<TranslationServiceInstance?> GetProviderInstanceForEnvironment(
        ITranslationServiceInstanceService translationServiceInstanceService,
        string provider,
        int instanceNumber,
        bool cascadeFromEnvironment,
        string environmentVariable)
    {
        var providerInstances = (await translationServiceInstanceService.GetInstances())
            .Where(instance => string.Equals(instance.Provider, provider, StringComparison.OrdinalIgnoreCase))
            .OrderBy(instance => instance.Id)
            .ToList();

        if (instanceNumber <= providerInstances.Count)
        {
            return providerInstances[instanceNumber - 1];
        }

        if (cascadeFromEnvironment)
        {
            _logger.LogWarning(
                "Environment variable {EnvironmentVariable} targets {Provider} instance {InstanceNumber}, which does not exist and is not created because {ServiceTypeVariable} is set.",
                environmentVariable,
                provider,
                instanceNumber,
                ServiceTypeEnvironmentVariable);
            return null;
        }

        if (instanceNumber > providerInstances.Count + 1)
        {
            _logger.LogWarning(
                "Environment variable {EnvironmentVariable} targets {Provider} instance {InstanceNumber}, but only {InstanceCount} exist. Only the next instance is created, skipping.",
                environmentVariable,
                provider,
                instanceNumber,
                providerInstances.Count);
            return null;
        }

        return await CreateInstanceFromEnvironment(translationServiceInstanceService, provider, environmentVariable);
    }

    private async Task<TranslationServiceInstance?> CreateInstanceFromEnvironment(
        ITranslationServiceInstanceService translationServiceInstanceService,
        string provider,
        string environmentVariable)
    {
        try
        {
            var instance = await translationServiceInstanceService.CreateInstance(provider, null);
            _logger.LogInformation(
                "Created translation service {Name} for provider {Provider} from environment variable {EnvironmentVariable}.",
                instance.Name,
                instance.Provider,
                environmentVariable);
            return instance;
        }
        catch (ArgumentException)
        {
            _logger.LogWarning(
                "Environment variable {EnvironmentVariable} names unknown translation service provider {Provider}, skipping.",
                environmentVariable,
                provider);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                "Environment variable {EnvironmentVariable} could not add provider {Provider}: {Reason}",
                environmentVariable,
                provider,
                exception.Message);
        }

        return null;
    }

    private static List<int> GetInstanceNumbers(string environmentVariable, IEnumerable<string> environmentVariableNames)
    {
        var pattern = new Regex($"^{Regex.Escape(environmentVariable)}_(\\d+)(_FILE)?$");
        var instanceNumbers = new SortedSet<int> { 1 };

        foreach (var name in environmentVariableNames)
        {
            var match = pattern.Match(name);
            if (match.Success
                && int.TryParse(match.Groups[1].Value, out var instanceNumber)
                && instanceNumber >= 2)
            {
                instanceNumbers.Add(instanceNumber);
            }
        }

        return instanceNumbers.ToList();
    }

    private static string? ReadEnvironmentValue(string environmentVariable)
    {
        return ReadFromFileEnv(environmentVariable + "_FILE") ?? Environment.GetEnvironmentVariable(environmentVariable);
    }

    private static string? ReadFromFileEnv(string fileEnvVar)
    {
        var path = Environment.GetEnvironmentVariable(fileEnvVar);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(path).TrimEnd('\r', '\n');
        }
        catch
        {
            return null;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
