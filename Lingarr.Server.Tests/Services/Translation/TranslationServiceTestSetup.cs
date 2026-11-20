using System;
using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Core.Data;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Plugins;
using Lingarr.Server.Services.Plugins.Manifests;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lingarr.Server.Tests.Services.Translation;

internal static class TranslationServiceTestSetup
{
    public static LingarrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LingarrDbContext(options);
    }

    public static PluginRegistry CreateBuiltInPluginRegistry()
    {
        IPluginManifest[] manifests =
        [
            new AnthropicPluginManifest(),
            new OpenAiPluginManifest(),
            new GeminiPluginManifest(),
            new DeepSeekPluginManifest(),
            new MistralPluginManifest(),
            new XAiPluginManifest(),
            new LocalAiPluginManifest(),
            new DeepLPluginManifest(),
            new LibreTranslatePluginManifest(),
            new GoogleTranslatePluginManifest(),
            new BingTranslatePluginManifest(),
            new MicrosoftTranslatePluginManifest(),
            new YandexTranslatePluginManifest()
        ];

        return new PluginRegistry(
            manifests,
            new PluginLoader(NullLogger<PluginLoader>.Instance),
            NullLogger<PluginRegistry>.Instance);
    }

    public static EncryptionService CreateEncryptionService()
    {
        return new EncryptionService(new EphemeralDataProtectionProvider());
    }
}
