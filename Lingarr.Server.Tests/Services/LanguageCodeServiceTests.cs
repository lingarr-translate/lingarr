using System;
using System.IO;
using System.Linq;
using Lingarr.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class LanguageCodeServiceTests
{
    private readonly LanguageCodeService _service;

    public LanguageCodeServiceTests()
    {
        var logger = NullLogger<LanguageCodeService>.Instance;
        _service = new LanguageCodeService(logger);
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
        var result = _service.GetNormalizedCode(code);
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
        var result = _service.GetNormalizedCode(code);
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
        var result = _service.GetNormalizedCode(code);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNormalizedCode_WithInvalidCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.GetNormalizedCode("invalid"));
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
        var normalizedCode = isValid ? _service.GetNormalizedCode(languagePart!) : null;

        Assert.True(isValid, $"Language part '{languagePart}' should be valid");
        Assert.Equal(expectedCode, normalizedCode);
    }

    #endregion
}
