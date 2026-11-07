using System.Globalization;
using System.Text.RegularExpressions;

namespace Lingarr.Server.Services.Subtitle;

/// <summary>
/// Converts the language tag and title of an embedded subtitle stream to the code Lingarr reads from file names.
/// </summary>
public class SubtitleStreamLanguageService
{
    private static readonly HashSet<string> UndefinedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "und", "unk", "mis", "mul", "zxx", "qaa"
    };

    // Matroska stores ISO 639-2/B codes, and some files carry deprecated or ISO 639-3
    // tags. .NET knows none of these, so they are mapped to a code it does know.
    private static readonly Dictionary<string, string> TagAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "alb", "sqi" }, { "arm", "hye" }, { "baq", "eus" }, { "bur", "mya" }, { "chi", "zho" },
        { "cze", "ces" }, { "dut", "nld" }, { "fre", "fra" }, { "geo", "kat" }, { "ger", "deu" },
        { "gre", "ell" }, { "ice", "isl" }, { "mac", "mkd" }, { "mao", "mri" }, { "may", "msa" },
        { "per", "fas" }, { "rum", "ron" }, { "slo", "slk" }, { "tib", "bod" }, { "wel", "cym" },
        { "scc", "srp" }, { "scr", "hrv" }, { "tgl", "fil" }, { "cmn", "zho" }, { "yue", "zh-HK" }
    };

    private static readonly Dictionary<string, string> LanguageNameAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "farsi", "fa" },
        { "tagalog", "fil" },
        { "mandarin", "zh" },
        { "flemish", "nl" }
    };

    private static readonly Dictionary<string, string> RegionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "latin american", "es-419" }, { "latin america", "es-419" }, { "latinoamérica", "es-419" },
        { "latinoamerica", "es-419" }, { "latinoamericano", "es-419" }, { "latino", "es-419" }, { "latam", "es-419" },
        { "castilian", "es-ES" }, { "castellano", "es-ES" }, { "spain", "es-ES" }, { "españa", "es-ES" },
        { "brazilian", "pt-BR" }, { "brazil", "pt-BR" }, { "brasil", "pt-BR" }, { "brasileiro", "pt-BR" },
        { "portugal", "pt-PT" },
        { "canadian", "fr-CA" }, { "canada", "fr-CA" }, { "québec", "fr-CA" }, { "quebec", "fr-CA" },
        { "simplified", "zh-Hans" }, { "简体", "zh-Hans" },
        { "traditional", "zh-Hant" }, { "繁體", "zh-Hant" }, { "繁体", "zh-Hant" },
        { "cantonese", "zh-HK" }, { "粵語", "zh-HK" }, { "粤语", "zh-HK" }
    };

    private static readonly Regex WordSeparator = new(@"[^\p{L}]+", RegexOptions.Compiled);
    private static readonly Regex RegionPattern = BuildRegionPattern();
    private static readonly Dictionary<string, string> CodeBySingleWordName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<(string Name, string Code)> MultiWordNames = [];

    private readonly LanguageCodeService _languageCodeService;

    static SubtitleStreamLanguageService()
    {
        BuildLanguageNameIndex();
    }

    public SubtitleStreamLanguageService(LanguageCodeService languageCodeService)
    {
        _languageCodeService = languageCodeService;
    }

    /// <summary>
    /// Resolves the language of a stream from its tag, or from a language name in its title.
    /// A region in the title, such as "Brazilian", narrows a language without a region.
    /// </summary>
    public string? ResolveLanguage(string? tag, string? title)
    {
        var language = NormalizeCode(tag) ?? FindLanguageInTitle(title);
        return language == null ? null : ApplyRegion(language, title);
    }

    public static IEnumerable<string> SplitWords(string text)
    {
        return WordSeparator.Split(text).Where(word => word.Length > 0);
    }

    private string? NormalizeCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var code = languageCode.Trim().Replace('_', '-');
        if (UndefinedCodes.Contains(code))
        {
            return null;
        }

        code = TagAliases.GetValueOrDefault(code, code);
        return _languageCodeService.Validate(code) ? LanguageCodeService.GetNormalizedCode(code) : null;
    }

    private string ApplyRegion(string language, string? title)
    {
        if (string.IsNullOrWhiteSpace(title) || language.Contains('-'))
        {
            return language;
        }

        foreach (Match match in RegionPattern.Matches(title))
        {
            var regionCode = RegionCodes[WhitespaceToSingleSpace(match.Value)];
            if (regionCode.Split('-')[0].Equals(language, StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeCode(regionCode) ?? language;
            }
        }

        return language;
    }

    private static string? FindLanguageInTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        // Region words are removed first, so "Latin American Spanish" reads as Spanish and not as Latin.
        var words = SplitWords(RegionPattern.Replace(title, " "))
            .Select(word => word.ToLowerInvariant())
            .ToList();

        var joinedWords = $" {string.Join(' ', words)} ";
        foreach (var (name, code) in MultiWordNames)
        {
            if (joinedWords.Contains($" {name} ", StringComparison.Ordinal))
            {
                return code;
            }
        }

        foreach (var word in words)
        {
            if (CodeBySingleWordName.TryGetValue(word, out var code))
            {
                return code;
            }
        }

        return null;
    }

    private static void BuildLanguageNameIndex()
    {
        foreach (var (name, code) in LanguageNameAliases)
        {
            CodeBySingleWordName[name] = code;
        }

        var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .Where(culture => !string.IsNullOrEmpty(culture.Name) && !culture.Name.Contains('-'));

        foreach (var culture in cultures)
        {
            var code = culture.Name.ToLowerInvariant();
            foreach (var name in new[] { culture.EnglishName, culture.NativeName })
            {
                var words = SplitWords(name).Select(word => word.ToLowerInvariant()).ToList();
                if (words.Count == 1)
                {
                    CodeBySingleWordName.TryAdd(words[0], code);
                }
                else if (words.Count > 1)
                {
                    MultiWordNames.Add((string.Join(' ', words), code));
                }
            }
        }
    }

    private static Regex BuildRegionPattern()
    {
        // CJK text has no spaces between words, so those names match anywhere in the title.
        var patterns = RegionCodes.Keys
            .OrderByDescending(key => key.Length)
            .Select(key =>
            {
                var pattern = Regex.Escape(key).Replace("\\ ", "\\s+");
                var isCjk = key.Any(character => char.GetUnicodeCategory(character) == UnicodeCategory.OtherLetter);
                return isCjk ? pattern : $@"\b{pattern}\b";
            });

        return new Regex(string.Join('|', patterns), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string WhitespaceToSingleSpace(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
