using System;
using System.Linq;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class TranslationServiceInstanceServiceTests : IDisposable
{
    private readonly LingarrDbContext _dbContext;
    private readonly EncryptionService _encryptionService;
    private readonly TranslationServiceInstanceService _service;

    public TranslationServiceInstanceServiceTests()
    {
        _dbContext = TranslationServiceTestSetup.CreateDbContext();
        _encryptionService = TranslationServiceTestSetup.CreateEncryptionService();

        _service = new TranslationServiceInstanceService(
            _dbContext,
            TranslationServiceTestSetup.CreateBuiltInPluginRegistry(),
            _encryptionService,
            NullLogger<TranslationServiceInstanceService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateInstance_UnknownProvider_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateInstance("unknown", null));
        Assert.Empty(await _dbContext.TranslationServiceInstances.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateInstance_ProviderWithoutSettingsAlreadyPresent_ThrowsInvalidOperationException()
    {
        await _service.CreateInstance("google", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateInstance("google", null));
    }

    [Fact]
    public async Task CreateInstance_ProviderWithSettingsAddedTwice_NumbersNameAndAppendsPosition()
    {
        var first = await _service.CreateInstance("OpenAI", null);
        var second = await _service.CreateInstance("openai", null);

        Assert.Equal("openai", first.Provider);
        Assert.Equal("OpenAI", first.Name);
        Assert.Equal(0, first.Position);
        Assert.Equal("openai", second.Provider);
        Assert.Equal("OpenAI 2", second.Name);
        Assert.Equal(1, second.Position);
    }

    [Fact]
    public async Task CreateInstance_GivenName_UsesTrimmedName()
    {
        var instance = await _service.CreateInstance("localai", "  Ollama at home ");

        Assert.Equal("Ollama at home", instance.Name);
    }

    [Fact]
    public async Task CreateInstance_ManifestDefaults_WritesNonSecretFieldsOnly()
    {
        var instance = await _service.CreateInstance("localai", null);

        var settings = await _service.GetInstanceSettings(instance.Id);

        Assert.NotNull(settings);
        Assert.Equal("http://ollama:11434/v1/chat/completions", settings[SettingKeys.Translation.LocalAi.Endpoint]);
        Assert.Equal("aya-expanse", settings[SettingKeys.Translation.LocalAi.Model]);
        Assert.Equal("true", settings[SettingKeys.Translation.LocalAi.StructuredOutput]);
        Assert.False(settings.ContainsKey(SettingKeys.Translation.LocalAi.ApiKey));
    }

    [Fact]
    public async Task DeleteInstance_LastInstance_ThrowsInvalidOperationException()
    {
        var instance = await _service.CreateInstance("deepl", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteInstance(instance.Id));
    }

    [Fact]
    public async Task DeleteInstance_MiddleInstance_RemovesSettingsAndRenumbersPositions()
    {
        var first = await _service.CreateInstance("openai", null);
        var second = await _service.CreateInstance("localai", null);
        var third = await _service.CreateInstance("deepl", null);

        Assert.True(await _service.DeleteInstance(second.Id));

        var instances = await _service.GetInstances();
        Assert.Equal([first.Id, third.Id], instances.Select(instance => instance.Id));
        Assert.Equal([0, 1], instances.Select(instance => instance.Position));
        Assert.False(await _dbContext.TranslationServiceInstanceSettings.AnyAsync(
            setting => setting.TranslationServiceInstanceId == second.Id,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReorderInstances_IdsNotMatchingAllInstances_ReturnsNull()
    {
        var first = await _service.CreateInstance("openai", null);
        var second = await _service.CreateInstance("deepl", null);

        Assert.Null(await _service.ReorderInstances([first.Id]));
        Assert.Null(await _service.ReorderInstances([first.Id, first.Id]));
        Assert.Null(await _service.ReorderInstances([first.Id, second.Id + 100]));
    }

    [Fact]
    public async Task ReorderInstances_AllIds_SetsPositionsInGivenOrder()
    {
        var first = await _service.CreateInstance("openai", null);
        var second = await _service.CreateInstance("deepl", null);
        var third = await _service.CreateInstance("localai", null);

        var reordered = await _service.ReorderInstances([third.Id, first.Id, second.Id]);

        Assert.NotNull(reordered);
        Assert.Equal([third.Id, first.Id, second.Id], reordered.Select(instance => instance.Id));
        Assert.Equal([0, 1, 2], reordered.Select(instance => instance.Position));
    }

    [Theory]
    [InlineData("openai", SettingKeys.Translation.DeepL.DeeplApiKey)]
    [InlineData("openai", SettingKeys.Translation.MaxRetries)]
    [InlineData("deepl", SettingKeys.Translation.AiPrompt)]
    public async Task SetInstanceSetting_KeyNotAllowedForProvider_ReturnsFalse(string provider, string key)
    {
        var instance = await _service.CreateInstance(provider, null);

        Assert.False(await _service.SetInstanceSetting(instance.Id, key, "value"));
    }

    [Theory]
    [InlineData("localai", SettingKeys.Translation.LocalAi.GenerateRequestTemplate)]
    [InlineData("openai", SettingKeys.Translation.AiPrompt)]
    public async Task SetInstanceSetting_AllowedKey_StoresValue(string provider, string key)
    {
        var instance = await _service.CreateInstance(provider, null);

        Assert.True(await _service.SetInstanceSetting(instance.Id, key, "value"));

        var settings = await _service.GetInstanceSettings(instance.Id);
        Assert.Equal("value", settings![key]);
    }

    [Fact]
    public async Task UnknownInstance_EveryMethod_ReturnsNotFound()
    {
        await _service.CreateInstance("openai", null);

        Assert.Null(await _service.RenameInstance(999, "name"));
        Assert.False(await _service.DeleteInstance(999));
        Assert.Null(await _service.GetInstanceSettings(999));
        Assert.False(await _service.SetInstanceSetting(999, SettingKeys.Translation.OpenAi.Model, "model"));
        Assert.False(await _service.DeleteInstancePrompt(999, SettingKeys.Translation.AiPrompt));
    }

    [Fact]
    public async Task SetInstanceSetting_SecretField_StoresEncryptedAndReturnsDecrypted()
    {
        var instance = await _service.CreateInstance("openai", null);

        Assert.True(await _service.SetInstanceSetting(instance.Id, SettingKeys.Translation.OpenAi.ApiKey, "secret-key"));

        var stored = await _dbContext.TranslationServiceInstanceSettings.SingleAsync(
            setting => setting.TranslationServiceInstanceId == instance.Id
                       && setting.Key == SettingKeys.Translation.OpenAi.ApiKey,
            TestContext.Current.CancellationToken);
        Assert.NotEqual("secret-key", stored.Value);

        var settings = await _service.GetInstanceSettings(instance.Id);
        Assert.NotNull(settings);
        Assert.Equal("secret-key", settings[SettingKeys.Translation.OpenAi.ApiKey]);
    }

    [Fact]
    public async Task SetInstanceSetting_ExistingKey_UpdatesValueOfThatInstanceOnly()
    {
        var first = await _service.CreateInstance("openai", null);
        var second = await _service.CreateInstance("openai", null);

        await _service.SetInstanceSetting(first.Id, SettingKeys.Translation.OpenAi.Model, "first-model");
        await _service.SetInstanceSetting(second.Id, SettingKeys.Translation.OpenAi.Model, "second-model");
        await _service.SetInstanceSetting(first.Id, SettingKeys.Translation.OpenAi.Model, "updated-model");

        var firstSettings = await _service.GetInstanceSettings(first.Id);
        var secondSettings = await _service.GetInstanceSettings(second.Id);
        Assert.Equal("updated-model", firstSettings![SettingKeys.Translation.OpenAi.Model]);
        Assert.Equal("second-model", secondSettings![SettingKeys.Translation.OpenAi.Model]);
    }

    [Fact]
    public async Task DeleteInstancePrompt_OwnPrompt_RemovesIt()
    {
        var openAi = await _service.CreateInstance("openai", null);
        await _service.SetInstanceSetting(openAi.Id, SettingKeys.Translation.ProofreadPrompt, "own prompt");

        var deleted = await _service.DeleteInstancePrompt(openAi.Id, SettingKeys.Translation.ProofreadPrompt);

        Assert.True(deleted);
        var settings = await _service.GetInstanceSettings(openAi.Id);
        Assert.False(settings!.ContainsKey(SettingKeys.Translation.ProofreadPrompt));
    }

    [Fact]
    public async Task EncryptPlaintextSecrets_PlaintextApiKey_EncryptsOnlyThatKey()
    {
        var openAi = await _service.CreateInstance("openai", null);
        _dbContext.TranslationServiceInstanceSettings.Add(new TranslationServiceInstanceSetting
        {
            TranslationServiceInstanceId = openAi.Id,
            Key = SettingKeys.Translation.OpenAi.ApiKey,
            Value = "sk-plaintext"
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.EncryptPlaintextSecrets();

        var rows = await _dbContext.TranslationServiceInstanceSettings
            .AsNoTracking()
            .Where(setting => setting.TranslationServiceInstanceId == openAi.Id)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, TestContext.Current.CancellationToken);
        Assert.True(_encryptionService.IsEncrypted(rows[SettingKeys.Translation.OpenAi.ApiKey]));
        Assert.Equal("sk-plaintext", _encryptionService.Decrypt(rows[SettingKeys.Translation.OpenAi.ApiKey]));
        Assert.False(_encryptionService.IsEncrypted(rows[SettingKeys.Translation.OpenAi.Model]));
    }

    [Fact]
    public async Task DeleteInstancePrompt_ProviderKey_IsRejected()
    {
        var openAi = await _service.CreateInstance("openai", null);

        var deleted = await _service.DeleteInstancePrompt(openAi.Id, SettingKeys.Translation.OpenAi.Model);

        Assert.False(deleted);
    }
}
