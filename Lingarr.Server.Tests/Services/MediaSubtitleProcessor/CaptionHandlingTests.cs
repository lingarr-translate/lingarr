using System.Collections.Generic;
using System.Threading.Tasks;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

/// <summary>
/// Tests for caption handling behavior (forced, hi, sdh, cc).
/// </summary>
public class CaptionHandlingTests : MediaSubtitleProcessorTestBase
{
    [Fact]
    public async Task ProcessMedia_WithForcedSubtitleOnly_ShouldProcessWhenIgnoreCaptionsTrue()
    {
        // Arrange - Only en.forced.srt exists, no plain en.srt
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
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

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - No translation needed because ro already exists as a regular subtitle
        Assert.False(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("en.forced.srt"))),
            Times.Never); // Should NOT create request because ro already exists
    }

    [Fact]
    public async Task ProcessMedia_WithBothForcedAndRegularSubtitles_ShouldPreferRegularWhenIgnoreCaptionsTrue()
    {
        // Arrange - Both en.forced.srt and en.srt exist
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.forced.srt",
                FileName = "test.movie",
                Language = "en",
                Caption = "forced",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.en.srt",
                FileName = "test.movie",
                Language = "en",
                Caption = "",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should prefer en.srt over en.forced.srt
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("test.movie.en.srt") &&
                !t.SubtitlePath.Contains("forced"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithHiSubtitleOnly_ShouldProcessWhenIgnoreCaptionsTrue()
    {
        // Arrange - Only en.hi.srt exists
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.hi.srt",
                FileName = "test.movie",
                Language = "en",
                Caption = "hi",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process because en.hi.srt is the only source available
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("en.hi.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithForcedSubtitle_ShouldAlwaysProcessWhenIgnoreCaptionsFalse()
    {
        // Arrange - en.forced.srt with ignoreCaptions disabled
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.forced.srt",
                FileName = "test.movie",
                Language = "en",
                Caption = "forced",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings(ignoreCaptions: "false");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process regardless of caption when ignoreCaptions is false
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("en.forced.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithSdhSubtitle_ShouldProcessWhenIgnoreCaptionsTrue()
    {
        // Arrange - Only en.sdh.srt exists
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
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

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process because en.sdh.srt is the only source available
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("en.sdh.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithCcSubtitle_ShouldProcessWhenIgnoreCaptionsTrue()
    {
        // Arrange - Only en.cc.srt exists
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.cc.srt",
                FileName = "test.movie.en.cc",
                Language = "en",
                Caption = "cc",
                Format = ".srt"
            }
        };

        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process because en.cc.srt is the only source available
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("en.cc.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithAllCaptionTypes_ShouldPreferNonCaption()
    {
        // Arrange - Test all caption types (forced, hi, sdh, cc) with a regular subtitle
        var movie = await CreateTestMovie();
        var subtitles = new List<Subtitles>
        {
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
                Path = "/movies/test/test.movie.en.hi.srt",
                FileName = "test.movie.en.hi",
                Language = "en",
                Caption = "hi",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.en.sdh.srt",
                FileName = "test.movie.en.sdh",
                Language = "en",
                Caption = "sdh",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.en.cc.srt",
                FileName = "test.movie.en.cc",
                Language = "en",
                Caption = "cc",
                Format = ".srt"
            },
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

        SetupStandardSettings();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should prefer the regular en.srt over all caption variants
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("test.movie.en.srt") &&
                !t.SubtitlePath.Contains("forced") &&
                !t.SubtitlePath.Contains(".hi.") &&
                !t.SubtitlePath.Contains("sdh") &&
                !t.SubtitlePath.Contains(".cc."))),
            Times.Once);
    }
}
