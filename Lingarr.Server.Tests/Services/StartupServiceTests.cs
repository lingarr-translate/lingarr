using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Plugins;
using Lingarr.Server.Services.Translation;
using Lingarr.Server.Tests.Services.Translation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

[Collection("StartupServiceEnv")]
public class StartupServiceTests : IDisposable
{
    private static readonly (string EnvVar, string SettingKey)[] EnvMap =
    {
        ("RADARR_URL", SettingKeys.Integration.RadarrUrl),
        ("RADARR_API_KEY", SettingKeys.Integration.RadarrApiKey),
        ("SONARR_URL", SettingKeys.Integration.SonarrUrl),
        ("SONARR_API_KEY", SettingKeys.Integration.SonarrApiKey),
        ("SOURCE_LANGUAGES", SettingKeys.Translation.SourceLanguages),
        ("TARGET_LANGUAGES", SettingKeys.Translation.TargetLanguages),
        ("USE_BATCH_TRANSLATION", SettingKeys.Translation.UseBatchTranslation),
        ("AI_PROMPT", SettingKeys.Translation.AiPrompt),
        ("AUTH_ENABLED", SettingKeys.Authentication.AuthEnabled),
        ("TELEMETRY_ENABLED", SettingKeys.Telemetry.TelemetryEnabled)
    };

    private static readonly string[] TranslationServicePrefixes =
    [
        "SERVICE_TYPE",
        "LIBRE_TRANSLATE_",
        "OPENAI_",
        "ANTHROPIC_",
        "LOCAL_AI_",
        "GEMINI_",
        "DEEPSEEK_",
        "MISTRAL_",
        "XAI_",
        "DEEPL_"
    ];

    private readonly string _tempDir;

    public StartupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lingarr-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        ClearEnv();
    }

    public void Dispose()
    {
        ClearEnv();
        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
            // ignore
        }
    }

    public static IEnumerable<object[]> EnvMapData() =>
        EnvMap.Select(entry => new object[] { entry.EnvVar, entry.SettingKey });

    [Theory]
    [MemberData(nameof(EnvMapData))]
    public async Task ApplySettingsFromEnvironment_FileVariableSet_PopulatesSetting(
        string envVar,
        string settingKey)
    {
        // Arrange
        var path = Path.Combine(_tempDir, envVar);
        await File.WriteAllTextAsync(path, "from-file-value", TestContext.Current.CancellationToken);
        Environment.SetEnvironmentVariable(envVar + "_FILE", path);

        var serviceProvider = BuildProvider();
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal("from-file-value", await GetSettingValue(serviceProvider, settingKey));
    }

    [Fact]
    public async Task ApplySettingsFromEnvironment_BothVariablesSet_PrefersFile()
    {
        // Arrange
        var settingKey = SettingKeys.Integration.RadarrUrl;
        var path = Path.Combine(_tempDir, "radarr_url");
        await File.WriteAllTextAsync(path, "from-file", TestContext.Current.CancellationToken);
        Environment.SetEnvironmentVariable("RADARR_URL", "from-env");
        Environment.SetEnvironmentVariable("RADARR_URL_FILE", path);

        var serviceProvider = BuildProvider();
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal("from-file", await GetSettingValue(serviceProvider, settingKey));
    }

    [Theory]
    [InlineData("value\n", "value")]
    [InlineData("value\r\n", "value")]
    [InlineData("value", "value")]
    [InlineData("value\n\n", "value")]
    public async Task ApplySettingsFromEnvironment_FileHasTrailingNewlines_StripsThem(
        string fileContent,
        string expectedValue)
    {
        // Arrange
        var settingKey = SettingKeys.Integration.RadarrUrl;
        var path = Path.Combine(_tempDir, "radarr_url_" + Guid.NewGuid().ToString("N"));
        await File.WriteAllTextAsync(path, fileContent, TestContext.Current.CancellationToken);
        Environment.SetEnvironmentVariable("RADARR_URL_FILE", path);

        var serviceProvider = BuildProvider();
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, await GetSettingValue(serviceProvider, settingKey));
    }

    [Fact]
    public async Task ApplySettingsFromEnvironment_FileMissing_FallsBackToDirectVariable()
    {
        // Arrange
        var settingKey = SettingKeys.Integration.RadarrUrl;
        Environment.SetEnvironmentVariable("RADARR_URL_FILE", Path.Combine(_tempDir, "does-not-exist"));
        Environment.SetEnvironmentVariable("RADARR_URL", "fallback-env-value");

        var serviceProvider = BuildProvider();
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal("fallback-env-value", await GetSettingValue(serviceProvider, settingKey));
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_ProviderVariables_SetSettingsOfFirstInstance()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        var firstId = await SeedInstance(serviceProvider, "openai", 0);
        await SeedInstance(serviceProvider, "openai", 1);
        Environment.SetEnvironmentVariable("OPENAI_MODEL", "gpt-from-env");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "secret-from-env");

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var settings = await GetInstanceSettings(serviceProvider, firstId);
        Assert.Equal("gpt-from-env", settings[SettingKeys.Translation.OpenAi.Model]);
        Assert.Equal("secret-from-env", settings[SettingKeys.Translation.OpenAi.ApiKey]);
        Assert.Equal(2, (await GetInstances(serviceProvider)).Count);

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LingarrDbContext>();
        var storedApiKey = await dbContext.TranslationServiceInstanceSettings.SingleAsync(
            setting => setting.TranslationServiceInstanceId == firstId
                       && setting.Key == SettingKeys.Translation.OpenAi.ApiKey,
            TestContext.Current.CancellationToken);
        Assert.NotEqual("secret-from-env", storedApiKey.Value);
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_NumberedFileVariable_CreatesAndConfiguresSecondInstance()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        var firstId = await SeedInstance(serviceProvider, "localai", 0);
        var path = Path.Combine(_tempDir, "local_ai_endpoint_2");
        await File.WriteAllTextAsync(path, "http://second:11434/api/generate\n", TestContext.Current.CancellationToken);
        Environment.SetEnvironmentVariable("LOCAL_AI_ENDPOINT_2_FILE", path);

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instances = await GetInstances(serviceProvider);
        Assert.Equal(2, instances.Count);
        Assert.Equal(firstId, instances[0].Id);
        Assert.Equal("localai", instances[1].Provider);
        Assert.Equal(1, instances[1].Position);

        var settings = await GetInstanceSettings(serviceProvider, instances[1].Id);
        Assert.Equal("http://second:11434/api/generate", settings[SettingKeys.Translation.LocalAi.Endpoint]);
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_NumberedVariableWithGap_DoesNotCreateInstances()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        await SeedInstance(serviceProvider, "libretranslate", 0);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY_3", "third-key");

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instance = Assert.Single(await GetInstances(serviceProvider));
        Assert.Equal("libretranslate", instance.Provider);
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_ProviderWithoutInstance_CreatesInstanceAtEnd()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        await SeedInstance(serviceProvider, "libretranslate", 0);
        Environment.SetEnvironmentVariable("DEEPL_API_KEY", "deepl-key");

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instances = await GetInstances(serviceProvider);
        Assert.Equal(["libretranslate", "deepl"], instances.Select(instance => instance.Provider));
        var settings = await GetInstanceSettings(serviceProvider, instances[1].Id);
        Assert.Equal("deepl-key", settings[SettingKeys.Translation.DeepL.DeeplApiKey]);
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_ServiceType_BuildsCascadeAndDeletesUnlisted()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        await SeedInstance(serviceProvider, "libretranslate", 0);
        var openAiId = await SeedInstance(serviceProvider, "openai", 1);
        var deepLId = await SeedInstance(serviceProvider, "deepl", 2);
        Environment.SetEnvironmentVariable("SERVICE_TYPE", "[\"deepl\",\"openai\",\"OpenAI\"]");

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instances = await GetInstances(serviceProvider);
        Assert.Equal(["deepl", "openai", "openai"], instances.Select(instance => instance.Provider));
        Assert.Equal([0, 1, 2], instances.Select(instance => instance.Position));
        Assert.Equal(deepLId, instances[0].Id);
        Assert.Equal(openAiId, instances[1].Id);
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_ServiceTypeSingleValue_KeepsOnlyThatProvider()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        await SeedInstance(serviceProvider, "libretranslate", 0);
        Environment.SetEnvironmentVariable("SERVICE_TYPE", "anthropic");

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instance = Assert.Single(await GetInstances(serviceProvider));
        Assert.Equal("anthropic", instance.Provider);
    }

    [Fact]
    public async Task ApplyTranslationServicesFromEnvironment_ServiceTypeSet_DoesNotCreateInstanceForProviderVariable()
    {
        // Arrange
        var serviceProvider = BuildProvider();
        await SeedInstance(serviceProvider, "openai", 0);
        Environment.SetEnvironmentVariable("SERVICE_TYPE", "openai");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "anthropic-key");
        Environment.SetEnvironmentVariable("OPENAI_MODEL_2", "second-model");

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instance = Assert.Single(await GetInstances(serviceProvider));
        Assert.Equal("openai", instance.Provider);
    }

    [Theory]
    [InlineData("[\"openai\"")]
    [InlineData("[1]")]
    [InlineData("[]")]
    [InlineData("unknownprovider")]
    public async Task ApplyTranslationServicesFromEnvironment_UnusableServiceType_KeepsInstances(string serviceType)
    {
        // Arrange
        var serviceProvider = BuildProvider();
        await SeedInstance(serviceProvider, "openai", 0);
        await SeedInstance(serviceProvider, "deepl", 1);
        Environment.SetEnvironmentVariable("SERVICE_TYPE", serviceType);

        // Act
        await new StartupService(serviceProvider, NullLogger<StartupService>.Instance).StartAsync(CancellationToken.None);

        // Assert
        var instances = await GetInstances(serviceProvider);
        Assert.Equal(["openai", "deepl"], instances.OrderBy(instance => instance.Position).Select(instance => instance.Provider));
    }

    private static void ClearEnv()
    {
        foreach (var (envVar, _) in EnvMap)
        {
            Environment.SetEnvironmentVariable(envVar, null);
            Environment.SetEnvironmentVariable(envVar + "_FILE", null);
        }

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var name = (string)entry.Key;
            if (TranslationServicePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }
    }

    private static IServiceProvider BuildProvider()
    {
        var databaseName = Guid.NewGuid().ToString();
        var settingServiceMock = new Mock<ISettingService>();
        settingServiceMock
            .Setup(settingService => settingService.SetSetting(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<LingarrDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IPluginRegistry>(TranslationServiceTestSetup.CreateBuiltInPluginRegistry());
        services.AddSingleton<IEncryptionService>(TranslationServiceTestSetup.CreateEncryptionService());
        services.AddSingleton(settingServiceMock.Object);
        services.AddScoped<ITranslationServiceInstanceService, TranslationServiceInstanceService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LingarrDbContext>();

        foreach (var (_, settingKey) in EnvMap)
        {
            dbContext.Settings.Add(new Setting { Key = settingKey, Value = "" });
        }

        dbContext.Settings.Add(new Setting { Key = SettingKeys.Integration.RadarrSettingsCompleted, Value = "false" });
        dbContext.Settings.Add(new Setting { Key = SettingKeys.Integration.SonarrSettingsCompleted, Value = "false" });
        dbContext.SaveChanges();

        return serviceProvider;
    }

    private static async Task<int> SeedInstance(IServiceProvider serviceProvider, string provider, int position)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LingarrDbContext>();
        var instance = new TranslationServiceInstance
        {
            Provider = provider,
            Name = provider,
            Position = position
        };
        dbContext.TranslationServiceInstances.Add(instance);
        await dbContext.SaveChangesAsync();
        return instance.Id;
    }

    private static async Task<List<TranslationServiceInstance>> GetInstances(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ITranslationServiceInstanceService>().GetInstances();
    }

    private static async Task<Dictionary<string, string>> GetInstanceSettings(IServiceProvider serviceProvider, int id)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var settings = await scope.ServiceProvider
            .GetRequiredService<ITranslationServiceInstanceService>()
            .GetInstanceSettings(id);
        Assert.NotNull(settings);
        return settings;
    }

    private static async Task<string?> GetSettingValue(IServiceProvider serviceProvider, string key)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LingarrDbContext>();
        var setting = await dbContext.Settings.FindAsync(key);
        return setting?.Value;
    }
}

[CollectionDefinition("StartupServiceEnv", DisableParallelization = true)]
public class StartupServiceEnvCollection
{
}
