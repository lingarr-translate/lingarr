using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class LocalAiPluginManifest : IPluginManifest
{
    public string Provider => "localai";

    public string DisplayName => "LocalAI / Ollama";

    public string? Description =>
        "Self-hosted OpenAI-compatible or Ollama-compatible deployments. The endpoint determines whether the chat/completions or the generate protocol is used; the API key is optional. Addresses usually consist of a path such as <code>/v1/chat/completions</code> or <code>/api/generate</code> and should follow the <a href='https://platform.openai.com/docs/api-reference/chat/create' target='_blank' rel='noopener'>OpenAI API specification</a>.";

    public bool HasRequestTemplate => true;

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.LocalAi.Endpoint,
            Label = "Address",
            Type = PluginSettingType.Url,
            Required = true,
            Default = "http://ollama:11434/v1/chat/completions",
            Description = "Full URL to the chat/completions or generate endpoint. Ending in 'completions' selects the OpenAI-compatible path."
        },
        new()
        {
            Key = SettingKeys.Translation.LocalAi.Model,
            Label = "AI Model",
            Type = PluginSettingType.Text,
            Required = true,
            Default = "aya-expanse",
            Description = "Model identifier the deployment exposes."
        },
        new()
        {
            Key = SettingKeys.Translation.LocalAi.ApiKey,
            Label = "API key (optional)",
            Type = PluginSettingType.Secret,
            Required = false,
            Description = "Bearer token if the deployment requires authentication. Stored encrypted."
        }
    ];
}
