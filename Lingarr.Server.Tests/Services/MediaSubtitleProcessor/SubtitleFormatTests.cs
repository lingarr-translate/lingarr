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
    public async Task ProcessMedia_WithMultipleFormats_ShouldProcessAll()
    {
        // Arrange - Multiple subtitle formats (.srt, .ass, .ssa)
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

        // Assert - Should process first matched subtitle (srt in this case)
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro")),
            Times.Once);
    }
}
