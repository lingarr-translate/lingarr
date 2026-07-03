using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class GeminiPluginManifest : IPluginManifest
{
    public string Provider => "gemini";

    public string DisplayName => "Google Gemini";

    public string? Description =>
        "Google's Gemini generative models. AI translation can be costly, only use it when you know what you are doing and keep automation disabled.";

    public bool HasRequestTemplate => true;

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.Gemini.ApiKey,
            Label = "API key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "Google AI Studio API key. Stored encrypted.",
            MinLength = 1,
            ValidationErrorMessage = "Value must not be empty"
        },
        new()
        {
            Key = SettingKeys.Translation.Gemini.Model,
            Label = "AI Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/gemini/models",
            Description = "Select a model from your Gemini catalogue."
        }
    ];
}
