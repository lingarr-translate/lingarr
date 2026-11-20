using System.Text.Json;

namespace Lingarr.Server.Services.Translation;

public static class TranslationServices
{
    /// <summary>
    /// Parses a SERVICE_TYPE value into a list of provider identifiers, or returns false when the value is empty or malformed.
    /// </summary>
    public static bool TryParse(string? raw, out List<string> providers)
    {
        providers = [];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!raw.TrimStart().StartsWith('['))
        {
            providers = [raw];
            return true;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string?>>(raw);
            if (parsed is null)
            {
                return false;
            }

            providers = parsed
                .Where(provider => !string.IsNullOrWhiteSpace(provider))
                .Select(provider => provider!)
                .ToList();
            return providers.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
