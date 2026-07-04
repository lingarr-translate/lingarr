using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class LibreTranslatePluginManifest : IPluginManifest
{
    public string Provider => "libretranslate";

    public string DisplayName => "LibreTranslate";

    public string? Description =>
        "Self-hosted or community LibreTranslate endpoint. Free public instances may or may not require an API key depending on your chosen configuration.";

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.LibreTranslate.Url,
            Label = "Address",
            Type = PluginSettingType.Url,
            Required = true,
            Description = "Base URL of the LibreTranslate deployment."
        },
        new()
        {
            Key = SettingKeys.Translation.LibreTranslate.ApiKey,
            Label = "API key (optional)",
            Type = PluginSettingType.Secret,
            Required = false,
            Description = "API key for deployments that require one. Stored encrypted."
        }
    ];
}
