using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class InstanceSettingServiceTests : IDisposable
{
    private static readonly HashSet<string> OpenAiInstanceKeys =
    [
        SettingKeys.Translation.OpenAi.ApiKey,
        SettingKeys.Translation.OpenAi.Model,
        SettingKeys.Translation.OpenAi.RequestTemplate
    ];

    private readonly LingarrDbContext _dbContext;
    private readonly Mock<ISettingService> _globalSettingServiceMock;
    private readonly EncryptionService _encryptionService;

    public InstanceSettingServiceTests()
    {
        _dbContext = TranslationServiceTestSetup.CreateDbContext();
        _encryptionService = TranslationServiceTestSetup.CreateEncryptionService();
        _globalSettingServiceMock = new Mock<ISettingService>();
        _globalSettingServiceMock
            .Setup(settingService => settingService.GetSetting(SettingKeys.Translation.MaxRetries))
            .ReturnsAsync("global retries");
        _globalSettingServiceMock
            .Setup(settingService => settingService.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> keys) => keys
                .Where(key => key == SettingKeys.Translation.AiPrompt || key == SettingKeys.Translation.MaxRetries)
                .ToDictionary(key => key, key => $"global:{key}"));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private async Task<int> SeedInstance(params (string Key, string Value)[] settings)
    {
        var instance = new TranslationServiceInstance
        {
            Provider = "openai",
            Name = "OpenAI",
            Position = _dbContext.TranslationServiceInstances.Count()
        };
        foreach (var (key, value) in settings)
        {
            instance.Settings.Add(new TranslationServiceInstanceSetting { Key = key, Value = value });
        }

        _dbContext.TranslationServiceInstances.Add(instance);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return instance.Id;
    }

    private InstanceSettingService CreateService(int instanceId)
    {
        return new InstanceSettingService(
            _globalSettingServiceMock.Object,
            _dbContext,
            _encryptionService,
            instanceId,
            OpenAiInstanceKeys);
    }

    [Theory]
    [InlineData(SettingKeys.Translation.OpenAi.Model, "gpt-instance")]
    [InlineData(SettingKeys.Translation.OpenAi.RequestTemplate, "")]
    [InlineData(SettingKeys.Translation.AiPrompt, "instance prompt")]
    [InlineData(SettingKeys.Translation.MaxRetries, "global retries")]
    public async Task GetSetting_Key_ReadsInstanceOrGlobalValue(string key, string expected)
    {
        await SeedInstance((SettingKeys.Translation.OpenAi.RequestTemplate, "other template"));
        var instanceId = await SeedInstance(
            (SettingKeys.Translation.OpenAi.Model, "gpt-instance"),
            (SettingKeys.Translation.AiPrompt, "instance prompt"));

        var value = await CreateService(instanceId).GetSetting(key);

        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task GetSettings_MixedKeys_CombinesInstanceAndGlobalValues()
    {
        var instanceId = await SeedInstance((SettingKeys.Translation.OpenAi.Model, "gpt-instance"));

        var settings = await CreateService(instanceId).GetSettings([
            SettingKeys.Translation.OpenAi.Model,
            SettingKeys.Translation.OpenAi.RequestTemplate,
            SettingKeys.Translation.AiPrompt,
            SettingKeys.Translation.MaxRetries
        ]);

        Assert.Equal("gpt-instance", settings[SettingKeys.Translation.OpenAi.Model]);
        Assert.Equal(string.Empty, settings[SettingKeys.Translation.OpenAi.RequestTemplate]);
        Assert.Equal($"global:{SettingKeys.Translation.AiPrompt}", settings[SettingKeys.Translation.AiPrompt]);
        Assert.Equal($"global:{SettingKeys.Translation.MaxRetries}", settings[SettingKeys.Translation.MaxRetries]);
        _globalSettingServiceMock.Verify(settingService => settingService.GetSettings(
            It.Is<IEnumerable<string>>(keys => !keys.Contains(SettingKeys.Translation.OpenAi.Model)
                                               && !keys.Contains(SettingKeys.Translation.OpenAi.RequestTemplate))),
            Times.Once);
    }

    [Fact]
    public async Task GetEncryptedSetting_InstanceSecret_ReturnsDecryptedValue()
    {
        var instanceId = await SeedInstance(
            (SettingKeys.Translation.OpenAi.ApiKey, _encryptionService.Encrypt("secret-key")));

        var value = await CreateService(instanceId).GetEncryptedSetting(SettingKeys.Translation.OpenAi.ApiKey);

        Assert.Equal("secret-key", value);
    }
}
