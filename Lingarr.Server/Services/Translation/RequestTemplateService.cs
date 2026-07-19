using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.RequestTemplates;

namespace Lingarr.Server.Services.Translation;

public class RequestTemplateService : IRequestTemplateService
{
    private static readonly Regex PlaceholderPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

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
        var result = PlaceholderPattern.Replace(template, match =>
            placeholders.TryGetValue(match.Groups[1].Value, out var value)
                ? JsonEncodedText.Encode(value).ToString()
                : match.Value);

        using var doc = JsonDocument.Parse(result);
        return result;
    }

    /// <inheritdoc />
    public string SetRequestFields(string requestBody, Dictionary<string, object?> fields)
    {
        if (JsonNode.Parse(requestBody) is not JsonObject body)
        {
            throw new JsonException("Request body is not a JSON object.");
        }

        foreach (var field in fields)
        {
            var value = JsonSerializer.SerializeToNode(field.Value);
            if (body[field.Key] is JsonObject existingObject && value is JsonObject valueObject)
            {
                MergeFields(existingObject, valueObject);
            }
            else
            {
                body[field.Key] = value;
            }
        }

        return body.ToJsonString();
    }

    private static void MergeFields(JsonObject target, JsonObject fields)
    {
        foreach (var field in fields)
        {
            // A JsonNode cannot belong to two parents, so detach via clone before assigning
            var value = field.Value?.DeepClone();
            if (target[field.Key] is JsonObject existingObject && value is JsonObject valueObject)
            {
                MergeFields(existingObject, valueObject);
            }
            else
            {
                target[field.Key] = value;
            }
        }
    }
}
