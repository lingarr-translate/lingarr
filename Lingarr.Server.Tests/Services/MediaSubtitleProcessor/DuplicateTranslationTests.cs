using System.Collections.Generic;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

/// <summary>
/// Tests for duplicate translation prevention (issue #312).
/// Ensures that languages with existing pending, in-progress, or completed
/// translation requests are not re-queued.
/// </summary>
public class DuplicateTranslationTests : MediaSubtitleProcessorTestBase
{
    [Fact]
    public async Task ProcessMedia_WithPendingRequest_ShouldNotCreateDuplicate()
    {
        // Arrange - Movie has only English subtitle, target is Romanian
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

        SetupStandardSettings(); // source=en, target=ro

        // Simulate an existing pending translation request for the same media and target language
        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "ro",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Pending
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should return false since a pending request already exists
        Assert.False(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMedia_WithInProgressRequest_ShouldNotCreateDuplicate()
    {
        // Arrange
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

        SetupStandardSettings();

        // Simulate an in-progress translation request
        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "ro",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.InProgress
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
    public async Task ProcessMedia_WithCompletedRequest_ShouldNotCreateDuplicate()
    {
        // Arrange
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

        SetupStandardSettings();

        // Simulate a completed translation request (e.g., file written with removeLanguageTag)
        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "ro",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Completed
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should not re-queue a completed translation
        Assert.False(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.IsAny<TranslateAbleSubtitle>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMedia_WithFailedRequest_ShouldCreateNewRequest()
    {
        // Arrange - A previously failed request should allow retry
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

        SetupStandardSettings();

        // Simulate a failed translation request - should allow retry
        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "ro",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Failed
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should create a new request since the previous one failed
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.TargetLanguage == "ro")),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_WithCancelledRequest_ShouldCreateNewRequest()
    {
        // Arrange - A cancelled request should allow re-creation
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

        SetupStandardSettings();

        // Simulate a cancelled translation request
        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "ro",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Cancelled
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should create a new request since the previous one was cancelled
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.TargetLanguage == "ro")),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMedia_PartialDuplicate_ShouldOnlyCreateMissingLanguages()
    {
        // Arrange - Movie needs fr and de, but fr already has a pending request
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

        // Configure two target languages
        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage>
            {
                new() { Code = "fr", Name = "French" },
                new() { Code = "de", Name = "German" }
            });

        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("false");

        // fr already has a pending request
        DbContext.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = movie.Id,
            Title = "Test Movie",
            SourceLanguage = "en",
            TargetLanguage = "fr",
            SubtitleToTranslate = "/movies/test/test.movie.en.srt",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Pending
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should only create request for "de", not "fr"
        Assert.True(result);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "de")),
            Times.Once);
        TranslationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t => t.TargetLanguage == "fr")),
            Times.Never);
    }
}
