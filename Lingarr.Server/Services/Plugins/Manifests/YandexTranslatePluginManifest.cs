using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class YandexTranslatePluginManifest : IPluginManifest
{
    public string Provider => "yandex";

    public string DisplayName => "Yandex Translate";

    public string? Description =>
        "Yandex Translate through the GTranslate library. No API key required, but rate limiting may cause failures.";

    public IReadOnlyList<PluginSettingField> Settings { get; } = [];
}
