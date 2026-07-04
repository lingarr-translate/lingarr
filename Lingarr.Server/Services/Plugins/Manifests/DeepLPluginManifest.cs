using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class DeepLPluginManifest : IPluginManifest
{
    public string Provider => "deepl";

    public string DisplayName => "DeepL";

    public string? Description =>
        "DeepL machine translation through the official SDK. Free and Pro keys are both supported. DeepL has <a href='https://developers.deepl.com/docs/resources/usage-limits' target='_blank' rel='noopener'>usage limits</a> and rate limits. A single subtitle file typically contains between 60,000 and 120,000 characters. To avoid exceeding these limits, keep automated translation disabled.";

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.DeepL.DeeplApiKey,
            Label = "API key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "DeepL API key (Free or Pro). Stored encrypted.",
            MinLength = 1,
            ValidationErrorMessage = "Value must not be empty"
        }
    ];
}
