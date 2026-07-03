using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class GoogleTranslatePluginManifest : IPluginManifest
{
    public string Provider => "google";

    public string DisplayName => "Google Translate";

    public string? Description =>
        "Google Translate through the GTranslate library. No API key required, but without rate limiting it may cause failures.";

    public IReadOnlyList<PluginSettingField> Settings { get; } = [];
}
