using System;
using System.IO;
using System.Linq;
using Lingarr.Contracts.Models;
using Lingarr.Server.Models;
using Lingarr.Server.Services;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class LanguageCodeServiceTests
{
    private readonly LanguageCodeService _service;

    public LanguageCodeServiceTests()
    {
        _service = new LanguageCodeService();
    }

    #region Validate Tests

    [Theory]
    [InlineData("en")]
    [InlineData("zh")]
    public void Validate_WithTwoLetterIsoCodes_ReturnsTrue(string code)
    {
        var result = _service.Validate(code);
        Assert.True(result);
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void Validate_WithRegionCodes_ReturnsTrue(string code)
    {
        var result = _service.Validate(code);
        Assert.True(result);
    }

    [Theory]
    [InlineData("zh-TW")]
    [InlineData("zh-CN")]
    [InlineData("ZH-TW")]
    public void Validate_WithLegacyChineseCodes_ReturnsTrue(string code)
    {
        var result = _service.Validate(code);
        Assert.True(result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithInvalidCodes_ReturnsFalse(string? code)
    {
        var result = _service.Validate(code);
        Assert.False(result);
    }

    #endregion

    #region GetCultureName Tests

    [Theory]
    [InlineData("en", "English")]
    [InlineData("zh", "Chinese")]
    public void GetCultureName_WithTwoLetterCodes_ReturnsCorrectName(string code, string expectedName)
    {
        var result = _service.GetCultureName(code);
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData("pt-BR", "Portuguese (Brazil)")]
    [InlineData("en-US", "English (United States)")]
    public void GetCultureName_WithRegionCodes_ReturnsCorrectName(string code, string expectedName)
    {
        var result = _service.GetCultureName(code);
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData("zh-TW", "Chinese (Traditional, Taiwan)")]
    [InlineData("zh-CN", "Chinese (Simplified, China)")]
    public void GetCultureName_WithLegacyChineseCodes_ReturnsCorrectName(string code, string expectedName)
    {
        var result = _service.GetCultureName(code);
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetCultureName_WithInvalidCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.GetCultureName("invalid"));
    }

    #endregion

    #region GetNormalizedCode Tests

    /// <summary>
    /// Check case normalization
    /// </summary>
    [Theory]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    public void GetNormalizedCode_WithTwoLetterCodes_ReturnsLowercase(string code, string expected)
    {
        var result = LanguageCodeService.GetNormalizedCode(code);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Check case normalization
    /// </summary>
    [Theory]
    [InlineData("pt-BR", "pt-br")]
    [InlineData("PT-BR", "pt-br")]
    public void GetNormalizedCode_WithRegionCodes_ReturnsLowercasePreservingRegion(string code, string expected)
    {
        var result = LanguageCodeService.GetNormalizedCode(code);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Check case normalization and format, i.e. preserves zh-cn format, NOT zh-hans-cn
    /// </summary>
    [Theory]
    [InlineData("zh-TW", "zh-tw")]
    [InlineData("ZH-TW", "zh-tw")] 
    [InlineData("zh-CN", "zh-cn")]
    public void GetNormalizedCode_WithLegacyChineseCodes_PreservesLegacyFormat(string code, string expected)
    {
        var result = LanguageCodeService.GetNormalizedCode(code);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNormalizedCode_WithInvalidCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => LanguageCodeService.GetNormalizedCode("invalid"));
    }

    #endregion

    #region GetBestMatch Tests

    private static void AssertMatch(LanguageMatch? match, string expectedCode, MatchTier expectedTier)
    {
        Assert.NotNull(match);
        Assert.Equal(expectedCode, match!.Code);
        Assert.Equal(expectedTier, match.Tier);
    }

    [Fact]
    public void GetBestMatch_ExactBeatsNeutral()
    {
        AssertMatch(_service.GetBestMatch("zh-TW", ["zh", "zh-CN", "zh-TW"]), "zh-TW", MatchTier.Exact);
    }

    [Fact]
    public void GetBestMatch_AliasEquivalentBeatsNeutral()
    {
        // Legacy zh-TW aliases to zh-Hant-TW; catalog has the canonical form.
        AssertMatch(_service.GetBestMatch("zh-TW", ["zh", "zh-Hant-TW"]), "zh-Hant-TW", MatchTier.AliasEquivalent);
    }

    [Fact]
    public void GetBestMatch_ScriptEquivalentBeatsNeutral()
    {
        AssertMatch(_service.GetBestMatch("zh-TW", ["zh-Hans", "zh-Hant"]), "zh-Hant", MatchTier.ScriptEquivalent);
    }

    [Fact]
    public void GetBestMatch_NeutralFallbackWhenNothingMoreSpecific()
    {
        AssertMatch(_service.GetBestMatch("zh-TW", ["zh", "zh-CN"]), "zh", MatchTier.NeutralEquivalent);
    }

    [Fact]
    public void GetBestMatch_DifferentScriptDoesNotMatch()
    {
        // zh-TW (Traditional) must not collapse to zh-CN (Simplified) — neither is an ancestor of the other.
        Assert.Null(_service.GetBestMatch("zh-TW", ["zh-CN"]));
    }

    [Fact]
    public void GetBestMatch_RegionSiblingDoesNotMatch()
    {
        // pt-BR and pt-PT share parent pt but neither is in the other's chain.
        Assert.Null(_service.GetBestMatch("pt-BR", ["pt-PT"]));
        Assert.Null(_service.GetBestMatch("pt-PT", ["pt-BR"]));
    }

    [Fact]
    public void GetBestMatch_NeutralBeatsSibling()
    {
        AssertMatch(_service.GetBestMatch("pt-BR", ["pt-PT", "pt"]), "pt", MatchTier.NeutralEquivalent);
    }

    [Fact]
    public void GetBestMatch_CaseInsensitiveExact()
    {
        AssertMatch(_service.GetBestMatch("ZH-tw", ["zh-TW"]), "zh-TW", MatchTier.Exact);
    }

    [Fact]
    public void GetBestMatch_NullOrUnknownRequestedReturnsNull()
    {
        Assert.Null(_service.GetBestMatch(null, ["en"]));
        Assert.Null(_service.GetBestMatch("notacode", ["en"]));
    }

    #endregion

    #region GetSupportedLanguages Tests

    [Fact]
    public void GetSupportedLanguages_IncludesNeutralsAndAllowedSpecifics()
    {
        var codes = _service.GetSupportedLanguages().Select(language => language.Code).ToHashSet();
        // Neutrals always included.
        Assert.Contains("en", codes);
        Assert.Contains("zh", codes);
        Assert.Contains("zh-Hans", codes);
        Assert.Contains("zh-Hant", codes);
        Assert.Contains("sr-Cyrl", codes);
        // Allowed regional variants.
        Assert.Contains("pt-BR", codes);
        Assert.Contains("pt-PT", codes);
        Assert.Contains("en-US", codes);
        Assert.Contains("en-GB", codes);
        Assert.Contains("zh-TW", codes);
    }

    [Fact]
    public void GetSupportedLanguages_ExcludesUnlistedRegionalVariants()
    {
        var codes = _service.GetSupportedLanguages().Select(language => language.Code).ToHashSet();
        // Regions that produce identical translation output as the neutral should be filtered.
        Assert.DoesNotContain("af-NA", codes);
        Assert.DoesNotContain("af-ZA", codes);
        Assert.DoesNotContain("en-CM", codes);
        Assert.DoesNotContain("en-KE", codes);
        Assert.DoesNotContain("fr-MC", codes);
    }

    [Fact]
    public void GetSupportedLanguages_AllEntriesHaveEmptyTargets()
    {
        Assert.All(_service.GetSupportedLanguages(), language => Assert.Empty(language.Targets));
    }

    #endregion

    #region Subtitle Detection Integration Test

    [Theory]
    [InlineData("Series - S02E01.zh.srt", "zh")]
    [InlineData("Series - S02E01.zh-TW.srt", "zh-tw")]
    [InlineData("Series - S02E01.zh-CN.srt", "zh-cn")]
    [InlineData("Series - S02E01.pt-BR.srt", "pt-br")]
    [InlineData("Series - S02E01.en.srt", "en")]
    [InlineData("Series - S02E01.nl.srt", "nl")]
    public void SubtitleFilenameDetection_ExtractsCorrectLanguageCode(string filename, string expectedCode)
    {
        // Simulate subtitle filename parsing logic
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        var parts = fileNameWithoutExtension.Split('.').Reverse().ToList();
        var languagePart = parts.FirstOrDefault();

        var isValid = languagePart != null && _service.Validate(languagePart);
        var normalizedCode = isValid ? LanguageCodeService.GetNormalizedCode(languagePart!) : null;

        Assert.True(isValid, $"Language part '{languagePart}' should be valid");
        Assert.Equal(expectedCode, normalizedCode);
    }

    #endregion
}
