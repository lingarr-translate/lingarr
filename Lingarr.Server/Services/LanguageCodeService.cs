using System.Globalization;

namespace Lingarr.Server.Services;

/// <summary>
/// Provides validation and conversion utilities for language codes.
/// Supports both two-letter ISO codes (en, pt, zh) and region-specific codes (pt-BR, nl-NL, zh-TW).
/// </summary>
public class LanguageCodeService
{
    private static readonly CultureInfo[] Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
    private readonly ILogger<LanguageCodeService> _logger;

    /// <summary>
    /// Mapping of legacy/common Chinese language codes to .NET CultureInfo codes.
    /// </summary>
    private static readonly Dictionary<string, string> LegacyChineseCodeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh-TW", "zh-Hant-TW" },  // Traditional Chinese (Taiwan)
        { "zh-CN", "zh-Hans-CN" },  // Simplified Chinese (China)
        { "zh-HK", "zh-Hant-HK" },  // Traditional Chinese (Hong Kong)
        { "zh-SG", "zh-Hans-SG" },  // Simplified Chinese (Singapore)
        { "zh-MO", "zh-Hant-MO" }   // Traditional Chinese (Macao)
    };

    public LanguageCodeService(ILogger<LanguageCodeService> logger)
    {
        _logger = logger;
    }

    public bool Validate(string? languageCode) => FindCulture(languageCode) != null;
    
    public string GetCultureName(string languageCode)
    {
        var culture = GetCulture(languageCode);
        return culture.EnglishName;
    }

    /// <summary>
    /// Finds the CultureInfo for a given language code, handling legacy Chinese codes.
    /// Returns null if the code is invalid or not found.
    /// </summary>
    private static CultureInfo? FindCulture(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var trimmedCode = languageCode.Trim();

        // Map legacy Chinese codes to .NET equivalents
        var normalizedCode = LegacyChineseCodeMapping.TryGetValue(trimmedCode, out var mapped)
            ? mapped
            : trimmedCode;

        return Cultures.FirstOrDefault(c =>
            string.Equals(normalizedCode, c.Name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedCode, c.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedCode, c.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the CultureInfo for a language code, throwing if invalid.
    /// </summary>
    private static CultureInfo GetCulture(string languageCode)
    {
        var culture = FindCulture(languageCode);
        if (culture == null)
        {
            throw new ArgumentException($"Invalid language code: '{languageCode}'", nameof(languageCode));
        }

        return culture;
    }

    /// <summary>
    /// Gets the matched culture code in lowercase, preserving region information.
    /// For legacy Chinese codes (zh-TW, zh-CN), returns them in their original format
    /// (e.g., "zh-tw", not "zh-hant-tw") for compatibility with subtitle files and translation services.
    /// </summary>
    public string GetNormalizedCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Language code cannot be empty", nameof(languageCode));
        }

        var trimmedCode = languageCode.Trim();

        // Preserve legacy Chinese codes in their original format
        if (LegacyChineseCodeMapping.ContainsKey(trimmedCode))
        {
            return trimmedCode.ToLowerInvariant();
        }

        var culture = GetCulture(trimmedCode);
        return culture.Name.ToLowerInvariant();
    }
}
