using System.Text.Json;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Translation;

public static class TranslationServices
{
    /// <summary>
    /// Parses a service_type setting value into a list of service identifiers.
    /// </summary>
    public static List<string> Parse(string? raw, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [SettingKeys.Translation.DefaultServiceType];
        }

        if (!raw.TrimStart().StartsWith('['))
        {
            return [raw];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(raw);
            if (parsed is { Count: > 0 })
            {
                return parsed;
            }
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(
                ex,
                "service_type setting contained malformed JSON, falling back to '{Default}'.",
                SettingKeys.Translation.DefaultServiceType);
        }

        return [SettingKeys.Translation.DefaultServiceType];
    }

    /// <summary>
    /// Returns the JSON array string for a service_type setting
    /// </summary>
    public static string Normalise(string? raw, ILogger? logger = null) =>
        JsonSerializer.Serialize(Parse(raw, logger));
}
