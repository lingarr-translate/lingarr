using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class XAiPluginManifest : IPluginManifest
{
    public string Provider => "xai";

    public string DisplayName => "xAI";

    public string? Description =>
        "xAI's OpenAI-compatible chat completion models. AI translation can be costly, only use it when you know what you are doing and keep automation disabled.";

    public bool HasRequestTemplate => true;

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.XAi.ApiKey,
            Label = "API key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "xAI API key. Stored encrypted.",
            MinLength = 1,
            ValidationErrorMessage = "Value must not be empty"
        },
        new()
        {
            Key = SettingKeys.Translation.XAi.Model,
            Label = "AI Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/xai/models",
            Description = "Select a model from your xAI catalogue."
        }
    ];
}
