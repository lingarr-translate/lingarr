using System.Collections.Generic;
using System.Threading.Tasks;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

/// <summary>
/// Tests for language-specific edge cases (Hindi, etc.).
/// </summary>
public class LanguageHandlingTests : MediaSubtitleProcessorTestBase
{
    [Fact]
    public async Task ProcessMedia_AllTargetLanguagesExist_ShouldReturnFalseAndNotCreateRequest()
    {
        // Arrange - Movie already has both source (en) and target (ro) subtitles
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.srt",
                FileName = "test.movie.en",
                Language = "en",
                Caption = "",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.ro.srt",
                FileName = "test.movie.ro",
                Language = "ro",
                Caption = "",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings(); // source=en, target=ro

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should return false since all target languages already exist
        Assert.False(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Never);

        // Assert - Hash should be persisted so subsequent runs short-circuit
        Assert.NotNull(movie.MediaHash);

        // Act - Second call should short-circuit via hash match at ProcessMedia line 64
        var secondResult = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Still false, and the subtitle processing logic was never reached again
        Assert.False(secondResult);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMedia_WithHindiLanguage_ShouldProcessCorrectly()
    {
        // Arrange - Hindi language subtitle (special case where "hi" could be confused with hearing impaired)
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.hi.srt",
                FileName = "test.movie.hi",
                Language = "hi", // Hindi language, not hearing impaired caption
                Caption = "",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "hi", Name = "Hindi" } });

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "en", Name = "English" } });

        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("true");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process Hindi language correctly
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "hi" &&
                t.TargetLanguage == "en" &&
                t.SubtitlePath.Contains("test.movie.hi.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithRegionalSourceAndNeutralSubtitle_ShouldUseNeutralSubtitle()
    {
        // Arrange - Source is en-US but the only subtitle is tagged en
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.srt",
                FileName = "test.movie.en",
                Language = "en",
                Caption = "",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en-US", Name = "English (United States)" } });

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "nl", Name = "Dutch" } });

        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("true");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should fall back to the en subtitle
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "nl" &&
                t.SubtitlePath.Contains("test.movie.en.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithRegionalSourceAndBothSubtitles_ShouldPreferExactSubtitle()
    {
        // Arrange - Source is pt-BR and both pt and pt-BR subtitles exist
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.pt.srt",
                FileName = "test.movie.pt",
                Language = "pt",
                Caption = "",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.pt-BR.srt",
                FileName = "test.movie.pt-BR",
                Language = "pt-br",
                Caption = "",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "pt-BR", Name = "Portuguese (Brazil)" } });

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "en", Name = "English" } });

        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("true");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should pick the pt-BR subtitle over pt
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "pt-br" &&
                t.SubtitlePath.Contains("test.movie.pt-BR.srt"))),
            Times.Once);
    }
}
