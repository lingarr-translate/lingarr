using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.RequestTemplates;

namespace Lingarr.Server.Services.Translation;

public class RequestTemplateService : IRequestTemplateService
{
    private readonly Dictionary<string, Func<string>> _templateFactories = new()
    {
        [SettingKeys.Translation.OpenAi.RequestTemplate] =
            () => JsonSerializer.Serialize(new OpenAiChatTemplate()),
        [SettingKeys.Translation.Anthropic.RequestTemplate] =
            () => JsonSerializer.Serialize(new AnthropicTemplate()),
        [SettingKeys.Translation.LocalAi.ChatRequestTemplate] =
            () => JsonSerializer.Serialize(new LocalAiChatTemplate()),
        [SettingKeys.Translation.LocalAi.GenerateRequestTemplate] =
            () => JsonSerializer.Serialize(new OllamaGenerateTemplate()),
        [SettingKeys.Translation.DeepSeek.RequestTemplate] =
            () => JsonSerializer.Serialize(new DeepSeekTemplate()),
        [SettingKeys.Translation.Gemini.RequestTemplate] =
            () => JsonSerializer.Serialize(new GeminiTemplate())
    };

    /// <inheritdoc />
    public Dictionary<string, string> GetDefaultTemplates()
    {
        return _templateFactories.ToDictionary(
            keyValuePair => keyValuePair.Key,
            keyValuePair => keyValuePair.Value()
        );
    }

    /// <inheritdoc />
    public string? GetDefaultTemplate(string settingKey)
    {
        if (!_templateFactories.TryGetValue(settingKey, out var factory))
        {
            return null;
        }

        return factory();
    }

    /// <inheritdoc />
    public string BuildRequestBody(string template, Dictionary<string, string> placeholders)
    {
        var result = placeholders.Aggregate(template, (current, placeholder) =>
        {
            return current.Replace(
                $"{{{placeholder.Key}}}",
                JsonEncodedText.Encode(placeholder.Value).ToString()
            );
        });

        using var doc = JsonDocument.Parse(result);
        return result;
    }
}
