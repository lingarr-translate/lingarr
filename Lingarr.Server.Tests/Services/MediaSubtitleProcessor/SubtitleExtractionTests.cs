using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

public class SubtitleExtractionTests : MediaSubtitleProcessorTestBase
{
    private static readonly Subtitles EnglishSubtitle = new()
    {
        Path = "/movies/test/test.movie.en.srt",
        FileName = "test.movie.en",
        Language = "en",
        Caption = "",
        Format = ".srt"
    };

    private string _mediaFileState = $"test.movie.mkv|{Guid.NewGuid():N}";

    [Fact]
    public async Task ProcessMedia_WithExtractionDisabled_DoesNotExtract()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([]);
        SubtitleExtractionServiceMock.Setup(service => service.IsAvailable()).Returns(true);

        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        Assert.False(result);
        VerifyExtraction(Times.Never());
    }

    [Fact]
    public async Task ProcessMedia_WhenFfmpegIsNotInstalled_DoesNotExtract()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([]);
        SetupExtraction(isAvailable: false);

        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        Assert.False(result);
        VerifyExtraction(Times.Never());
    }

    [Fact]
    public async Task ProcessMedia_WithNothingToExtract_StoresHashAndSkipsNextRun()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([]);
        SetupExtraction(isAvailable: true);

        await Processor.ProcessMedia(movie, MediaType.Movie);
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        Assert.False(result);
        Assert.False(string.IsNullOrEmpty(movie.MediaHash));
        VerifyExtraction(Times.Once());
    }

    [Fact]
    public async Task ProcessMedia_WhenExtractionWritesSubtitle_CreatesTranslationRequest()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SubtitleServiceMock
            .SetupSequence(service => service.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([])
            .ReturnsAsync([EnglishSubtitle]);
        SetupExtraction(isAvailable: true);
        SubtitleExtractionServiceMock
            .Setup(service => service.ExtractSubtitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            service => service.CreateRequest(It.Is<TranslateAbleSubtitle>(subtitle =>
                subtitle.SourceLanguage == "en" &&
                subtitle.TargetLanguage == "ro" &&
                subtitle.SubtitlePath == EnglishSubtitle.Path)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WhenExtractionFails_DoesNotStoreHash()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([]);
        SetupExtraction(isAvailable: true);
        SubtitleExtractionServiceMock
            .Setup(service => service.ExtractSubtitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ffmpeg failed"));

        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        Assert.False(result);
        Assert.True(string.IsNullOrEmpty(movie.MediaHash));
    }

    [Fact]
    public async Task ProcessMedia_WhenExtractionFailsWithExternalSubtitle_RetriesNextRun()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([EnglishSubtitle]);
        SetupExtraction(isAvailable: true);
        SubtitleExtractionServiceMock
            .Setup(service => service.ExtractSubtitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ffmpeg failed"));

        await Processor.ProcessMedia(movie, MediaType.Movie);
        await Processor.ProcessMedia(movie, MediaType.Movie);

        Assert.True(string.IsNullOrEmpty(movie.MediaHash));
        VerifyExtraction(Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessMedia_AfterThreeFailures_SkipsUntilVideoFileChanges()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([]);
        SetupExtraction(isAvailable: true);
        SubtitleExtractionServiceMock
            .Setup(service => service.ExtractSubtitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ffmpeg failed"));

        for (var run = 0; run < 4; run++)
        {
            await Processor.ProcessMedia(movie, MediaType.Movie);
        }

        VerifyExtraction(Times.Exactly(3));
        Assert.False(string.IsNullOrEmpty(movie.MediaHash));

        _mediaFileState = $"test.movie.mkv|{Guid.NewGuid():N}";
        await Processor.ProcessMedia(movie, MediaType.Movie);

        VerifyExtraction(Times.Exactly(4));
    }

    [Fact]
    public async Task ProcessMedia_WhenVideoFileChanges_ExtractsAgain()
    {
        var movie = await CreateTestMovie();
        SetupStandardSettings();
        SetupSubtitles([]);
        SetupExtraction(isAvailable: true);

        await Processor.ProcessMedia(movie, MediaType.Movie);
        _mediaFileState = "test.movie.mkv|2000|2";
        await Processor.ProcessMedia(movie, MediaType.Movie);

        VerifyExtraction(Times.Exactly(2));
    }

    private void SetupSubtitles(List<Subtitles> subtitles)
    {
        SubtitleServiceMock
            .Setup(service => service.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles);
    }

    private void SetupExtraction(bool isAvailable)
    {
        SettingServiceMock
            .Setup(service => service.GetSetting(SettingKeys.SubtitleExtraction.ExtractionEnabled))
            .ReturnsAsync("true");
        SubtitleExtractionServiceMock.Setup(service => service.IsAvailable()).Returns(isAvailable);
        SubtitleExtractionServiceMock
            .Setup(service => service.GetMediaFileState(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => _mediaFileState);
    }

    private void VerifyExtraction(Times times)
    {
        SubtitleExtractionServiceMock.Verify(
            service => service.ExtractSubtitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            times);
    }
}
