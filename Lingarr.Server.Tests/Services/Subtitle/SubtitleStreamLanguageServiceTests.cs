using Lingarr.Server.Services;
using Lingarr.Server.Services.Subtitle;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

public class SubtitleStreamLanguageServiceTests
{
    private readonly SubtitleStreamLanguageService _service = new(new LanguageCodeService());

    [Theory]
    [InlineData("eng", "en")]
    [InlineData("en", "en")]
    [InlineData("en_GB", "en-gb")]
    [InlineData("fra", "fr")]
    [InlineData("fre", "fr")]
    [InlineData("ger", "de")]
    [InlineData("dut", "nl")]
    [InlineData("chi", "zh")]
    [InlineData("pt-BR", "pt-br")]
    [InlineData("es-419", "es-419")]
    [InlineData("zh-Hant", "zh-hant")]
    [InlineData("zh-CN", "zh-cn")]
    [InlineData("zh-TW", "zh-tw")]
    [InlineData("zh-HK", "zh-hk")]
    [InlineData("yue", "zh-hk")]
    [InlineData("cmn", "zh")]
    [InlineData("tgl", "fil")]
    [InlineData("scc", "sr")]
    [InlineData("scr", "hr")]
    [InlineData("haw", "haw")]
    public void ResolveLanguage_WithKnownTag_ReturnsLingarrCode(string tag, string expected)
    {
        Assert.Equal(expected, _service.ResolveLanguage(tag, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("und")]
    [InlineData("mul")]
    [InlineData("zxx")]
    [InlineData("xyz123")]
    public void ResolveLanguage_WithUndefinedTag_ReturnsNull(string? tag)
    {
        Assert.Null(_service.ResolveLanguage(tag, null));
    }

    [Theory]
    [InlineData("English SDH", "en")]
    [InlineData("Français", "fr")]
    [InlineData("Deutsch (Forced)", "de")]
    [InlineData("Spanish", "es")]
    [InlineData("Farsi", "fa")]
    [InlineData("Tagalog", "fil")]
    [InlineData("Tiếng Việt", "vi")]
    [InlineData("Norwegian Bokmål", "nb")]
    [InlineData("中文", "zh")]
    [InlineData("日本語", "ja")]
    [InlineData("Brazilian Portuguese", "pt-br")]
    [InlineData("Portuguese (Brazil)", "pt-br")]
    [InlineData("Português (Brasil)", "pt-br")]
    [InlineData("Spanish (Latin America)", "es-419")]
    [InlineData("Latin American Spanish", "es-419")]
    [InlineData("Español (Latinoamérica)", "es-419")]
    [InlineData("Castilian Spanish", "es-es")]
    [InlineData("French (Canada)", "fr-ca")]
    [InlineData("Chinese (Simplified)", "zh-hans")]
    [InlineData("Chinese Traditional", "zh-hant")]
    [InlineData("简体中文", "zh-hans")]
    [InlineData("繁體中文", "zh-hant")]
    [InlineData("Mandarin", "zh")]
    public void ResolveLanguage_FromTitle_ReturnsCode(string title, string expected)
    {
        Assert.Equal(expected, _service.ResolveLanguage(null, title));
    }

    [Theory]
    [InlineData("Full")]
    [InlineData("Signs & Songs")]
    [InlineData("Commentary")]
    [InlineData("Director's Commentary")]
    [InlineData("Standard")]
    [InlineData("Western")]
    [InlineData("Central")]
    [InlineData("Northern")]
    [InlineData("Southern")]
    [InlineData("Upper")]
    [InlineData("Lower")]
    [InlineData("Swiss")]
    [InlineData("Brazilian")]
    [InlineData("Simplified")]
    public void ResolveLanguage_FromTitleWithoutLanguageName_ReturnsNull(string title)
    {
        Assert.Null(_service.ResolveLanguage(null, title));
    }

    [Theory]
    [InlineData("por", "Brazilian", "pt-br")]
    [InlineData("por", "European", "pt")]
    [InlineData("spa", "Latino", "es-419")]
    [InlineData("chi", "Simplified", "zh-hans")]
    [InlineData("chi", "Cantonese", "zh-hk")]
    [InlineData("fre", "Canadian", "fr-ca")]
    [InlineData("eng", "Brazilian", "en")]
    [InlineData("zh-TW", "Simplified", "zh-tw")]
    [InlineData("eng", "Français", "en")]
    public void ResolveLanguage_WithTagAndTitle_PrefersTagAndAppliesMatchingRegion(string tag, string title, string expected)
    {
        Assert.Equal(expected, _service.ResolveLanguage(tag, title));
    }
}
