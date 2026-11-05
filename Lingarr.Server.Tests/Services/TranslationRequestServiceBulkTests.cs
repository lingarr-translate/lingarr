using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

/// <summary>
/// Tests for how bulk translation handles the ignoreCaptions and first target only settings.
/// </summary>
public class TranslationRequestServiceBulkTests : IDisposable
{
    private readonly LingarrDbContext _dbContext;
    private readonly Mock<ISettingService> _settingServiceMock;
    private readonly Mock<ISubtitleService> _subtitleServiceMock;
    private readonly TranslationRequestService _translationRequestService;

    public TranslationRequestServiceBulkTests()
    {
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new LingarrDbContext(options);

        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });
        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage>());

        var subtitleService = new SubtitleService(
            new Mock<ILogger<SubtitleService>>().Object,
            new LanguageCodeService());
        _subtitleServiceMock = new Mock<ISubtitleService>();
        _subtitleServiceMock
            .Setup(s => s.SelectSourceSubtitle(
                It.IsAny<List<Subtitles>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns((List<Subtitles> subs, IReadOnlyList<string> sources) =>
                subtitleService.SelectSourceSubtitle(subs, sources));
        _subtitleServiceMock
            .Setup(s => s.TargetSubtitleExists(
                It.IsAny<List<Subtitles>>(),
                It.IsAny<string>(),
                It.IsAny<Subtitles>(),
                It.IsAny<bool>()))
            .Returns((List<Subtitles> subs, string target, Subtitles source, bool ignoreCaptions) =>
                subtitleService.TargetSubtitleExists(subs, target, source, ignoreCaptions));

        _translationRequestService = new TranslationRequestService(
            _dbContext,
            new Mock<IBackgroundJobClient>().Object,
            new Mock<IHubContext<TranslationRequestsHub>> { DefaultValue = DefaultValue.Mock }.Object,
            new Mock<ITranslationServiceFactory>().Object,
            new Mock<IProgressService>().Object,
            new Mock<IStatisticsService>().Object,
            new Mock<IMediaService>().Object,
            _settingServiceMock.Object,
            _subtitleServiceMock.Object,
            new Mock<ITranslationRequestEventService>().Object,
            NullLogger<TranslationRequestService>.Instance);
    }

    [Fact]
    public async Task CreateBulkRequest_WithCaptionedTarget_ShouldSkipWhenIgnoreCaptionsTrue()
    {
        // Arrange - en.srt exists and the only ro subtitle is ro.forced.srt
        var movie = await CreateTestMovie();
        SetupSubtitles();
        SetupIgnoreCaptions("true");

        // Act
        await _translationRequestService.CreateBulkRequest(new BulkTranslateRequest
        {
            MediaIds = [movie.Id],
            TargetLanguage = "ro",
            MediaType = MediaType.Movie
        });

        // Assert - Should skip because a ro subtitle with a caption exists
        Assert.Empty(_dbContext.TranslationRequests);
    }

    [Fact]
    public async Task CreateBulkRequest_WithCaptionedTarget_ShouldTranslateWhenIgnoreCaptionsFalse()
    {
        // Arrange - en.srt exists and the only ro subtitle is ro.forced.srt
        var movie = await CreateTestMovie();
        SetupSubtitles();
        SetupIgnoreCaptions("false");

        // Act
        await _translationRequestService.CreateBulkRequest(new BulkTranslateRequest
        {
            MediaIds = [movie.Id],
            TargetLanguage = "ro",
            MediaType = MediaType.Movie
        });

        // Assert - Should translate because ro.forced.srt does not count as a regular ro subtitle
        var translationRequest = Assert.Single(_dbContext.TranslationRequests);
        Assert.Equal("/movies/test/test.movie.en.srt", translationRequest.SubtitleToTranslate);
        Assert.Equal("ro", translationRequest.TargetLanguage);
    }

    [Fact]
    public async Task CreateBulkRequest_WithOtherConfiguredTargetExisting_ShouldSkipWhenFirstTargetOnlyTrue()
    {
        // Arrange - ro is requested and nl, another configured target, already exists
        var movie = await CreateTestMovie();
        SetupSubtitlesWithExistingTarget("nl");
        SetupIgnoreCaptions("false");
        SetupTargetLanguages("ro", "nl");
        SetupFirstTargetOnly("true");

        // Act
        await _translationRequestService.CreateBulkRequest(new BulkTranslateRequest
        {
            MediaIds = [movie.Id],
            TargetLanguage = "ro",
            MediaType = MediaType.Movie
        });

        // Assert
        Assert.Empty(_dbContext.TranslationRequests);
    }

    [Fact]
    public async Task CreateBulkRequest_WithUnconfiguredTarget_ShouldTranslateWhenFirstTargetOnlyTrue()
    {
        // Arrange - de is not a configured target, so the first target only setting does not apply
        var movie = await CreateTestMovie();
        SetupSubtitlesWithExistingTarget("nl");
        SetupIgnoreCaptions("false");
        SetupTargetLanguages("ro", "nl");
        SetupFirstTargetOnly("true");

        // Act
        await _translationRequestService.CreateBulkRequest(new BulkTranslateRequest
        {
            MediaIds = [movie.Id],
            TargetLanguage = "de",
            MediaType = MediaType.Movie
        });

        // Assert
        var translationRequest = Assert.Single(_dbContext.TranslationRequests);
        Assert.Equal("de", translationRequest.TargetLanguage);
    }

    [Fact]
    public async Task CreateBulkRequest_WithOtherConfiguredTargetExisting_ShouldTranslateWhenFirstTargetOnlyFalse()
    {
        // Arrange
        var movie = await CreateTestMovie();
        SetupSubtitlesWithExistingTarget("nl");
        SetupIgnoreCaptions("false");
        SetupTargetLanguages("ro", "nl");
        SetupFirstTargetOnly("false");

        // Act
        await _translationRequestService.CreateBulkRequest(new BulkTranslateRequest
        {
            MediaIds = [movie.Id],
            TargetLanguage = "ro",
            MediaType = MediaType.Movie
        });

        // Assert
        var translationRequest = Assert.Single(_dbContext.TranslationRequests);
        Assert.Equal("ro", translationRequest.TargetLanguage);
    }

    private async Task<Movie> CreateTestMovie()
    {
        var movie = new Movie
        {
            Id = 1,
            RadarrId = 1,
            Title = "Test Movie",
            Path = "/movies/test",
            FileName = "test.movie",
            MediaHash = null,
            DateAdded = DateTime.UtcNow
        };
        await _dbContext.Movies.AddAsync(movie);
        await _dbContext.SaveChangesAsync();
        return movie;
    }

    private void SetupSubtitles()
    {
        _subtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Subtitles>
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
                    Path = "/movies/test/test.movie.ro.forced.srt",
                    FileName = "test.movie.ro.forced",
                    Language = "ro",
                    Caption = "forced",
                    Format = ".srt"
                }
            });
    }

    private void SetupSubtitlesWithExistingTarget(string targetLanguage)
    {
        _subtitleServiceMock
            .Setup(s => s.GetSubtitles(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Subtitles>
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
                    Path = $"/movies/test/test.movie.{targetLanguage}.srt",
                    FileName = $"test.movie.{targetLanguage}",
                    Language = targetLanguage,
                    Caption = "",
                    Format = ".srt"
                }
            });
    }

    private void SetupTargetLanguages(params string[] targetCodes)
    {
        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(targetCodes.Select(code => new TargetLanguage { Code = code, Name = code }).ToList());
    }

    private void SetupFirstTargetOnly(string firstTargetOnly)
    {
        _settingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.FirstTargetOnly))
            .ReturnsAsync(firstTargetOnly);
    }

    private void SetupIgnoreCaptions(string ignoreCaptions)
    {
        _settingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync(ignoreCaptions);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
