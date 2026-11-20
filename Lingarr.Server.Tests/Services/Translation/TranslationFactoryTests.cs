using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Plugins;
using Lingarr.Contracts.Settings;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Plugins;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class TranslationFactoryTests : IDisposable
{
    private const string PluginProvider = "fakeplugin";
    private const string PluginSettingKey = "fakeplugin_endpoint";

    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly LingarrDbContext _dbContext;
    private readonly TranslationFactory _factory;

    public TranslationFactoryTests()
    {
        var builtInRegistry = TranslationServiceTestSetup.CreateBuiltInPluginRegistry();
        var pluginRegistryMock = new Mock<IPluginRegistry>();
        pluginRegistryMock
            .Setup(registry => registry.Find(It.IsAny<string>()))
            .Returns((string provider) => string.Equals(provider, PluginProvider, StringComparison.OrdinalIgnoreCase)
                ? new RegisteredPlugin
                {
                    Manifest = new FakePluginManifest(),
                    IsBuiltIn = false,
                    SourceFile = "FakePlugin.dll",
                    ImplementationType = typeof(FakePluginTranslationService)
                }
                : builtInRegistry.Find(provider));

        var globalSettingServiceMock = new Mock<ISettingService>();
        globalSettingServiceMock
            .Setup(settingService => settingService.GetSetting(SettingKeys.Translation.AiPrompt))
            .ReturnsAsync("global prompt");

        var databaseContext = TranslationServiceTestSetup.CreateDbContext();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => databaseContext);
        services.AddSingleton(pluginRegistryMock.Object);
        services.AddSingleton(globalSettingServiceMock.Object);
        services.AddSingleton<IEncryptionService>(TranslationServiceTestSetup.CreateEncryptionService());
        services.AddSingleton<LanguageCodeService>();
        services.AddSingleton(new Mock<IRequestTemplateService>().Object);
        services.AddScoped<ITranslationServiceInstanceService, TranslationServiceInstanceService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<LingarrDbContext>();
        _factory = new TranslationFactory(_scope.ServiceProvider, NullLogger<TranslationFactory>.Instance);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<TranslationServiceInstance> SeedInstance(
        string provider,
        int position,
        params (string Key, string Value)[] settings)
    {
        var instance = new TranslationServiceInstance
        {
            Provider = provider,
            Name = provider,
            Position = position
        };
        foreach (var (key, value) in settings)
        {
            instance.Settings.Add(new TranslationServiceInstanceSetting { Key = key, Value = value });
        }

        _dbContext.TranslationServiceInstances.Add(instance);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return instance;
    }

    [Fact]
    public async Task CreateTranslationService_PluginProvider_ReadsSettingsOfItsInstance()
    {
        var first = await SeedInstance(PluginProvider, 0, (PluginSettingKey, "http://first"));
        var second = await SeedInstance(PluginProvider, 1, (PluginSettingKey, "http://second"));

        var firstService = Assert.IsType<FakePluginTranslationService>(_factory.CreateTranslationService(first));
        var secondService = Assert.IsType<FakePluginTranslationService>(_factory.CreateTranslationService(second));

        Assert.Equal("http://first", await firstService.SettingsAccess.GetSettingAsync(PluginSettingKey));
        Assert.Equal("http://second", await secondService.SettingsAccess.GetSettingAsync(PluginSettingKey));
        Assert.Equal("global prompt", await firstService.SettingsAccess.GetSettingAsync(SettingKeys.Translation.AiPrompt));
    }

    [Fact]
    public async Task CreateTranslationService_UnknownProvider_ThrowsArgumentException()
    {
        var instance = await SeedInstance("removedplugin", 0);

        Assert.Throws<ArgumentException>(() => _factory.CreateTranslationService(instance));
    }

    [Fact]
    public async Task CreateTranslationServices_UnknownProvider_SkipsItAndKeepsOrder()
    {
        var deepL = await SeedInstance("deepl", 0);
        var removed = await SeedInstance("removedplugin", 1);
        var openAi = await SeedInstance("openai", 2);

        var entries = _factory.CreateTranslationServices([deepL, removed, openAi]);

        Assert.Equal(2, entries.Count);
        Assert.Equal("deepl", entries[0].Name);
        Assert.IsType<DeepLService>(entries[0].Service);
        Assert.Equal("openai", entries[1].Name);
        Assert.IsType<OpenAiService>(entries[1].Service);
        Assert.NotNull(entries[1].BatchService);
    }

    public sealed class FakePluginManifest : IPluginManifest
    {
        public string Provider => PluginProvider;
        public string DisplayName => "Fake plugin";
        public string? Description => null;

        public IReadOnlyList<PluginSettingField> Settings { get; } =
        [
            new()
            {
                Key = PluginSettingKey,
                Label = "Endpoint",
                Type = PluginSettingType.Url
            }
        ];
    }

    public sealed class FakePluginTranslationService : ITranslationService
    {
        public FakePluginTranslationService(ISettingsAccess settingsAccess)
        {
            SettingsAccess = settingsAccess;
        }

        public ISettingsAccess SettingsAccess { get; }

        public string? ModelName => null;

        public Task<string> TranslateAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            List<string>? contextLinesBefore,
            List<string>? contextLinesAfter,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(text);
        }

        public Task<List<SourceLanguage>> GetLanguages()
        {
            return Task.FromResult(new List<SourceLanguage>());
        }

        public Task<ModelsResponse> GetModels()
        {
            return Task.FromResult(new ModelsResponse());
        }

        public Task<LanguagePair?> GetLanguagePair(
            string requestedSource,
            string requestedTarget,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<LanguagePair?>(null);
        }
    }
}
