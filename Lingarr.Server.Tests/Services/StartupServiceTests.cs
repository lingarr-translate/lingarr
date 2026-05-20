using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
        ("LIBRE_TRANSLATE_URL", SettingKeys.Translation.LibreTranslate.Url),
        ("LIBRE_TRANSLATE_API_KEY", SettingKeys.Translation.LibreTranslate.ApiKey),
        ("AI_PROMPT", SettingKeys.Translation.AiPrompt),
        ("OPENAI_MODEL", SettingKeys.Translation.OpenAi.Model),
        ("OPENAI_API_KEY", SettingKeys.Translation.OpenAi.ApiKey),
        ("ANTHROPIC_MODEL", SettingKeys.Translation.Anthropic.Model),
        ("ANTHROPIC_API_KEY", SettingKeys.Translation.Anthropic.ApiKey),
        ("ANTHROPIC_VERSION", SettingKeys.Translation.Anthropic.Version),
        ("LOCAL_AI_MODEL", SettingKeys.Translation.LocalAi.Model),
        ("LOCAL_AI_API_KEY", SettingKeys.Translation.LocalAi.ApiKey),
        ("LOCAL_AI_ENDPOINT", SettingKeys.Translation.LocalAi.Endpoint),
        ("GEMINI_MODEL", SettingKeys.Translation.Gemini.Model),
        ("GEMINI_API_KEY", SettingKeys.Translation.Gemini.ApiKey),
        ("DEEPSEEK_MODEL", SettingKeys.Translation.DeepSeek.Model),
        ("DEEPSEEK_API_KEY", SettingKeys.Translation.DeepSeek.ApiKey),
        ("DEEPL_API_KEY", SettingKeys.Translation.DeepL.DeeplApiKey),
        ("AUTH_ENABLED", SettingKeys.Authentication.AuthEnabled),
        ("TELEMETRY_ENABLED", SettingKeys.Telemetry.TelemetryEnabled)
    };

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
        await File.WriteAllTextAsync(path, "from-file-value");
        Environment.SetEnvironmentVariable(envVar + "_FILE", path);

        var serviceProvider = BuildProvider();
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal("from-file-value", await GetSettingValue(serviceProvider, settingKey));
    }

    [Fact]
    public async Task ApplySettingsFromEnvironment_ServiceType_NormalisesToJsonArray()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "SERVICE_TYPE");
        await File.WriteAllTextAsync(path, "openai");
        Environment.SetEnvironmentVariable("SERVICE_TYPE_FILE", path);

        var serviceProvider = BuildProvider();
        await using (var seedScope = serviceProvider.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<LingarrDbContext>();
            seedDb.Settings.Add(new Setting { Key = SettingKeys.Translation.ServiceType, Value = "" });
            await seedDb.SaveChangesAsync();
        }
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert: a legacy single-value env var is wrapped into a one-element JSON array.
        Assert.Equal("[\"openai\"]", await GetSettingValue(serviceProvider, SettingKeys.Translation.ServiceType));
    }

    [Fact]
    public async Task ApplySettingsFromEnvironment_BothVariablesSet_PrefersFile()
    {
        // Arrange
        var settingKey = SettingKeys.Translation.OpenAi.ApiKey;
        var path = Path.Combine(_tempDir, "openai_api_key");
        await File.WriteAllTextAsync(path, "from-file");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "from-env");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY_FILE", path);

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
        var settingKey = SettingKeys.Translation.OpenAi.ApiKey;
        var path = Path.Combine(_tempDir, "openai_api_key_" + Guid.NewGuid().ToString("N"));
        await File.WriteAllTextAsync(path, fileContent);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY_FILE", path);

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
        var settingKey = SettingKeys.Translation.OpenAi.ApiKey;
        Environment.SetEnvironmentVariable("OPENAI_API_KEY_FILE", Path.Combine(_tempDir, "does-not-exist"));
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "fallback-env-value");

        var serviceProvider = BuildProvider();
        var startupService = new StartupService(serviceProvider, NullLogger<StartupService>.Instance);

        // Act
        await startupService.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal("fallback-env-value", await GetSettingValue(serviceProvider, settingKey));
    }

    private static void ClearEnv()
    {
        foreach (var (envVar, _) in EnvMap)
        {
            Environment.SetEnvironmentVariable(envVar, null);
            Environment.SetEnvironmentVariable(envVar + "_FILE", null);
        }
    }

    private static IServiceProvider BuildProvider()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<LingarrDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

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
