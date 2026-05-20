using System.Globalization;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services;

/// <summary>
/// Validation and matching utilities for translation-service language codes.
/// Handles ISO-639-1, ISO-639-2, BCP-47 region codes, and legacy Chinese aliases.
/// </summary>
public class LanguageCodeService
{
    private static readonly Dictionary<string, CultureInfo> CultureByCode = BuildCultureIndex();

    private static readonly Dictionary<string, string> LegacyChineseAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh-TW", "zh-Hant-TW" },  // Traditional Chinese (Taiwan)
        { "zh-CN", "zh-Hans-CN" },  // Simplified Chinese (China)
        { "zh-HK", "zh-Hant-HK" },  // Traditional Chinese (Hong Kong)
        { "zh-SG", "zh-Hans-SG" },  // Simplified Chinese (Singapore)
        { "zh-MO", "zh-Hant-MO" }   // Traditional Chinese (Macao)
    };

    private static readonly HashSet<string> AllowedSpecificCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "zh-CN", "zh-TW", "zh-HK", "zh-SG", "zh-MO",
        "pt-BR", "pt-PT", "pt-AO", "pt-MZ",
        "es-ES", "es-MX", "es-419", "es-AR", "es-CL", "es-CO", "es-PE", "es-VE",
        "fr-FR", "fr-CA", "fr-BE", "fr-CH", "fr-LU",
        "de-DE", "de-AT", "de-CH", "de-LU", "de-LI",
        "en-US", "en-GB", "en-AU", "en-CA", "en-NZ", "en-IE", "en-IN", "en-ZA", "en-SG",
        "nl-NL", "nl-BE",
        "it-IT", "it-CH",
        "ar-EG", "ar-SA", "ar-MA", "ar-AE", "ar-LB", "ar-IQ", "ar-DZ", "ar-TN",
        "ru-RU", "ru-BY", "ru-KZ", "ru-UA"
    };

    private static readonly IReadOnlyList<SourceLanguage> SupportedLanguagesCache = BuildSupportedLanguages();

    public bool Validate(string? languageCode) => FindCulture(languageCode) != null;

    public string GetCultureName(string languageCode) =>
        FindCulture(languageCode)?.EnglishName
        ?? throw new ArgumentException($"Invalid language code: '{languageCode}'", nameof(languageCode));

    /// <summary>
    /// Retrieves the list of language cultures shown in the language picker.
    /// </summary>
    /// <returns>A list of neutral cultures and a curated set of region-specific variants, each with an empty Targets list</returns>
    public IReadOnlyList<SourceLanguage> GetSupportedLanguages() => SupportedLanguagesCache;

    /// <summary>
    /// Gets the matched culture code in lowercase. Legacy Chinese codes (zh-CN, zh-TW, ...)
    /// are preserved in their original format for compatibility with subtitle files and translation services.
    /// </summary>
    public static string GetNormalizedCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Language code cannot be empty", nameof(languageCode));
        }

        if (LegacyChineseAliases.ContainsKey(languageCode))
        {
            return languageCode.ToLowerInvariant();
        }

        return FindCulture(languageCode)?.Name.ToLowerInvariant()
            ?? throw new ArgumentException($"Invalid language code: '{languageCode}'", nameof(languageCode));
    }

    private static IReadOnlyList<SourceLanguage> BuildSupportedLanguages()
    {
        var fromCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures)
            .Where(culture => !string.IsNullOrEmpty(culture.Name))
            .Where(culture => culture.IsNeutralCulture || AllowedSpecificCultures.Contains(culture.Name))
            .Select(culture => new SourceLanguage
            {
                Code = culture.Name,
                Name = culture.EnglishName,
                Targets = []
            });

        var fromAliases = LegacyChineseAliases.Keys
            .Select(alias => new SourceLanguage
            {
                Code = alias,
                Name = FindCulture(alias)?.EnglishName ?? alias,
                Targets = []
            });

        return fromCultures.Concat(fromAliases)
            .GroupBy(language => language.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(language => language.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Picks the best candidate match for the requested code. Returns null when no candidate matches.
    /// </summary>
    public LanguageMatch? GetBestMatch(string? requestedCode, IEnumerable<string> candidateCodes)
    {
        var requestedCulture = FindCulture(requestedCode);
        if (requestedCulture == null)
        {
            return null;
        }

        var requestedRaw = (requestedCode ?? string.Empty).Trim();
        LanguageMatch? best = null;

        foreach (var candidate in candidateCodes)
        {
            var candidateCulture = FindCulture(candidate);
            if (candidateCulture == null)
            {
                continue;
            }

            var candidateTier = GetMatchTier(requestedRaw, requestedCulture, candidate.Trim(), candidateCulture);
            if (candidateTier is null) continue;

            if (best is { } current && candidateTier.Value >= current.Tier)
            {
                continue;
            }
            best = new LanguageMatch { Code = candidate, Tier = candidateTier.Value };
            if (candidateTier == MatchTier.Exact)
            {
                break;
            }
        }

        return best;
    }

    /// <summary>
    /// Classifies how closely a candidate matches the requested culture, or returns null
    /// when the two cultures are unrelated.
    /// </summary>
    private static MatchTier? GetMatchTier(
        string requestedRaw,
        CultureInfo requested,
        string candidateRaw,
        CultureInfo candidate)
    {
        if (string.Equals(requestedRaw, candidateRaw, StringComparison.OrdinalIgnoreCase))
        {
            return MatchTier.Exact;
        }

        if (requested.Equals(candidate))
        {
            return MatchTier.AliasEquivalent;
        }

        CultureInfo ancestor;
        if (IsAncestorOf(candidate, requested))
        {
            ancestor = candidate;
        }
        else if (IsAncestorOf(requested, candidate))
        {
            ancestor = requested;
        }
        else
        {
            return null;
        }

        // A dash in the ancestor's name means it still carries a script or region subtag (zh-Hant),
        // which is a closer match than collapsing to the bare language root (zh).
        return ancestor.Name.Contains('-') ? MatchTier.ScriptEquivalent : MatchTier.NeutralEquivalent;
    }

    private static bool IsAncestorOf(CultureInfo ancestor, CultureInfo descendant)
    {
        for (var current = descendant.Parent; !string.IsNullOrEmpty(current.Name); current = current.Parent)
        {
            if (current.Equals(ancestor))
            {
                return true;
            }
        }
        return false;
    }

    private static CultureInfo? FindCulture(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var legacyChineseAlias = LegacyChineseAliases.GetValueOrDefault(languageCode, languageCode);
        return CultureByCode.GetValueOrDefault(legacyChineseAlias);
    }

    private static Dictionary<string, CultureInfo> BuildCultureIndex()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        var cultureMap = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in cultures)
        {
            if (!string.IsNullOrEmpty(culture.Name))
            {
                cultureMap[culture.Name] = culture;
            }
        }

        // TryAdd ISO aliases without overwriting names from the initial loop.
        foreach (var culture in cultures)
        {
            if (!string.IsNullOrEmpty(culture.TwoLetterISOLanguageName))
            {
                cultureMap.TryAdd(culture.TwoLetterISOLanguageName, culture);
            }

            if (!string.IsNullOrEmpty(culture.ThreeLetterISOLanguageName))
            {
                cultureMap.TryAdd(culture.ThreeLetterISOLanguageName, culture);
            }
        }

        return cultureMap;
    }
}
