using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class BingTranslatePluginManifest : IPluginManifest
{
    public string Provider => "bing";
    public string DisplayName => "Bing Translate";
    public string? Description =>
        "Bing Translate through the GTranslate library. No API key required, but without rate limiting it may cause failures.";
    public IReadOnlyList<PluginSettingField> Settings { get; } = [];
}
