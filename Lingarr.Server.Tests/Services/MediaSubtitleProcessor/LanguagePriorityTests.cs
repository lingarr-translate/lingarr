using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

/// <summary>
/// Tests for source language priority and the first target only setting.
/// </summary>
public class LanguagePriorityTests : MediaSubtitleProcessorTestBase
{
    [Fact]
    public async Task ProcessMedia_WithEarlierSourceWeakerMatch_ShouldPreferEarlierSource()
    {
        // Arrange - en-US only matches the neutral en subtitle, nl matches exactly
        var movie = await CreateTestMovie();
        SetupSubtitles(
            CreateSubtitle("en"),
            CreateSubtitle("nl"));
        SetupLanguages(["en-US", "nl"], ["de"]);

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - The first source language in the list wins
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "de" &&
                t.SubtitlePath.Contains("test.movie.en.srt"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithLaterTargetExisting_ShouldSkipWhenFirstTargetOnlyTrue()
    {
        // Arrange - nl is the second target and already exists
        var movie = await CreateTestMovie();
        SetupSubtitles(
            CreateSubtitle("en"),
            CreateSubtitle("nl"));
        SetupLanguages(["en"], ["ro", "nl"]);
        SetupFirstTargetOnly("true");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert
        Assert.False(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Never);
        Assert.NotNull(movie.MediaHash);
    }

    [Fact]
    public async Task ProcessMedia_WithNoTargetExisting_ShouldTranslateFirstTargetWhenFirstTargetOnlyTrue()
    {
        // Arrange
        var movie = await CreateTestMovie();
        SetupSubtitles(CreateSubtitle("en"));
        SetupLanguages(["en"], ["nl", "ro"]);
        SetupFirstTargetOnly("true");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "nl")),
            Times.Once);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithForcedLaterTarget_ShouldTranslateFirstTargetWhenFirstTargetOnlyTrue()
    {
        // Arrange - The only nl subtitle is forced, which does not count as an existing target
        var movie = await CreateTestMovie();
        SetupSubtitles(
            CreateSubtitle("en"),
            CreateSubtitle("nl", "forced"));
        SetupLanguages(["en"], ["ro", "nl"], ignoreCaptions: "false");
        SetupFirstTargetOnly("true");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "ro")),
            Times.Once);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithActiveRequestForLaterTarget_ShouldSkipWhenFirstTargetOnlyTrue()
    {
        // Arrange - A pending request exists for the second target
        var movie = await CreateTestMovie();
        SetupSubtitles(CreateSubtitle("en"));
        SetupLanguages(["en"], ["ro", "nl"]);
        SetupFirstTargetOnly("true");

        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "nl",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Pending
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert
        Assert.False(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMedia_WithFirstTargetExisting_ShouldTranslateEveryMissingTargetWhenFirstTargetOnlyFalse()
    {
        // Arrange - nl exists, ro and de are missing
        var movie = await CreateTestMovie();
        SetupSubtitles(
            CreateSubtitle("en"),
            CreateSubtitle("nl"));
        SetupLanguages(["en"], ["nl", "ro", "de"]);
        SetupFirstTargetOnly("false");

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "ro")),
            Times.Once);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "de")),
            Times.Once);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "nl")),
            Times.Never);
    }

    private static Subtitles CreateSubtitle(string language, string caption = "")
    {
        var fileName = string.IsNullOrEmpty(caption)
            ? $"test.movie.{language}"
            : $"test.movie.{language}.{caption}";
        return new Subtitles
        {
            Path = $"/movies/test/{fileName}.srt",
            FileName = fileName,
            Language = language,
            Caption = caption,
            Format = ".srt"
        };
    }

    private void SetupSubtitles(params Subtitles[] subtitles)
    {
        SubtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(subtitles.ToList());
    }

    private void SetupLanguages(string[] sourceCodes, string[] targetCodes, string ignoreCaptions = "true")
    {
        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(sourceCodes.Select(code => new SourceLanguage { Code = code, Name = code }).ToList());

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(targetCodes.Select(code => new TargetLanguage { Code = code, Name = code }).ToList());

        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync(ignoreCaptions);
    }

    private void SetupFirstTargetOnly(string firstTargetOnly)
    {
        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.FirstTargetOnly))
            .ReturnsAsync(firstTargetOnly);
    }
}
