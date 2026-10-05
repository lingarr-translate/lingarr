using System.Collections.Generic;
using System.Threading.Tasks;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

/// <summary>
/// Tests for subtitle format support (.srt, .ssa, .ass).
/// </summary>
public class SubtitleFormatTests : MediaSubtitleProcessorTestBase
{
    [Fact]
    public async Task ProcessMedia_WithAssFormat_ShouldProcess()
    {
        // Arrange - .ass format subtitle
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.ass",
                FileName = "test.movie.en",
                Language = "en",
                Caption = "",
                Format = ".ass"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process .ass format
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("test.movie.en.ass") &&
                t.SubtitleFormat == ".ass")),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithSsaFormat_ShouldProcess()
    {
        // Arrange - .ssa format subtitle
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.ssa",
                FileName = "test.movie.en",
                Language = "en",
                Caption = "",
                Format = ".ssa"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process .ssa format
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("test.movie.en.ssa") &&
                t.SubtitleFormat == ".ssa")),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithMultipleFormats_ShouldProcessOnlyOneFormat()
    {
        // Arrange - Multiple subtitle formats with the same caption
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
                Path = "/movies/test/test.movie.en.ass",
                FileName = "test.movie.en",
                Language = "en",
                Caption = "",
                Format = ".ass"
            },
            new()
            {
                Path = "/movies/test/test.movie.en.ssa",
                FileName = "test.movie.en",
                Language = "en",
                Caption = "",
                Format = ".ssa"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Only one format should be translated for the same caption
        Assert.True(result);

        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                string.IsNullOrEmpty(t.Caption))),
            Times.Once);

        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithMultipleCaptionVariants_ShouldProcessEachCaption()
    {
        // Arrange - Different caption variants should each be translated
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
                Path = "/movies/test/test.movie.en.forced.srt",
                FileName = "test.movie.en.forced",
                Language = "en",
                Caption = "forced",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.en.sdh.srt",
                FileName = "test.movie.en.sdh",
                Language = "en",
                Caption = "sdh",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings("false");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Each caption variant should be translated separately
        Assert.True(result);

        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.Caption == "" &&
                t.SubtitlePath.Contains("test.movie.en.srt"))),
            Times.Once);

        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.Caption == "forced" &&
                t.SubtitlePath.Contains("test.movie.en.forced.srt"))),
            Times.Once);

        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.Caption == "sdh" &&
                t.SubtitlePath.Contains("test.movie.en.sdh.srt"))),
            Times.Once);

        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Exactly(3));
    }
}
