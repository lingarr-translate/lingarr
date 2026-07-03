using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class MicrosoftTranslatePluginManifest : IPluginManifest
{
    public string Provider => "microsoft";

    public string DisplayName => "Microsoft Translate";

    public string? Description =>
        "Microsoft Translate through the GTranslate library. No API key required, but rate limiting may cause failures.";

    public IReadOnlyList<PluginSettingField> Settings { get; } = [];
}
