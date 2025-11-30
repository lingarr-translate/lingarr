using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class MediaSubtitleProcessorTests
{
    private readonly Mock<ITranslationRequestService> _translationRequestServiceMock;
    private readonly Mock<ILogger<IMediaSubtitleProcessor>> _loggerMock;
    private readonly Mock<ISubtitleService> _subtitleServiceMock;
    private readonly Mock<ISettingService> _settingServiceMock;
    private readonly LingarrDbContext _dbContext;
    private readonly MediaSubtitleProcessor _processor;

    public MediaSubtitleProcessorTests()
    {
        _translationRequestServiceMock = new Mock<ITranslationRequestService>();
        _loggerMock = new Mock<ILogger<IMediaSubtitleProcessor>>();
        _subtitleServiceMock = new Mock<ISubtitleService>();
        _settingServiceMock = new Mock<ISettingService>();

        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        _dbContext = new LingarrDbContext(options);

        _processor = new MediaSubtitleProcessor(
            _translationRequestServiceMock.Object,
            _loggerMock.Object,
            _settingServiceMock.Object,
            _subtitleServiceMock.Object,
            _dbContext);
    }

    [Fact]
    public async Task ProcessMedia_WithForcedSubtitleOnly_ShouldProcessWhenIgnoreCaptionsTrue()
    {
        // Arrange - Only en.forced.srt exists, no plain en.srt
        var movie = new Movie
        {
            Id = 1,
            RadarrId = 1,
            Title = "Test Movie",
            Path = "/movies/test",
            FileName = "test.movie",
            MediaHash = null,
            DateAdded = System.DateTime.UtcNow
        };
        await _dbContext.Movies.AddAsync(movie);
        await _dbContext.SaveChangesAsync();

        var subtitles = new List<Subtitles>
        {
            new()
            {
                Path = "/movies/test/test.movie.en.forced.srt",
                FileName = "test.movie.en.forced",  // This is filename WITHOUT extension
                Language = "en",
                Caption = "forced",
                Format = ".srt"
            },
            new()
            {
                Path = "/movies/test/test.movie.ro.srt",
                FileName = "test.movie.ro",  // This is filename WITHOUT extension
                Language = "ro",
                Caption = "",
                Format = ".srt"
            }
        };

        _subtitleServiceMock
            .Setup(s => s.GetAllSubtitles(It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "ro", Name = "Romanian" } });

        _settingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("true");

        // Act
        var result = await _processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process because en.forced.srt is the only source available
        Assert.True(result);
        _translationRequestServiceMock.Verify(
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
        var movie = new Movie
        {
            Id = 1,
            RadarrId = 1,
            Title = "Test Movie",
            Path = "/movies/test",
            FileName = "test.movie",
            MediaHash = null,
            DateAdded = System.DateTime.UtcNow
        };
        await _dbContext.Movies.AddAsync(movie);
        await _dbContext.SaveChangesAsync();

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

        _subtitleServiceMock
            .Setup(s => s.GetAllSubtitles(It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "ro", Name = "Romanian" } });

        _settingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("true");

        // Act
        var result = await _processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should prefer en.srt over en.forced.srt
        Assert.True(result);
        _translationRequestServiceMock.Verify(
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
        var movie = new Movie
        {
            Id = 1,
            RadarrId = 1,
            Title = "Test Movie",
            Path = "/movies/test",
            FileName = "test.movie",
            MediaHash = null,
            DateAdded = System.DateTime.UtcNow
        };
        await _dbContext.Movies.AddAsync(movie);
        await _dbContext.SaveChangesAsync();

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

        _subtitleServiceMock
            .Setup(s => s.GetAllSubtitles(It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "ro", Name = "Romanian" } });

        _settingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("true");

        // Act
        var result = await _processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process because en.hi.srt is the only source available
        Assert.True(result);
        _translationRequestServiceMock.Verify(
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
        var movie = new Movie
        {
            Id = 1,
            RadarrId = 1,
            Title = "Test Movie",
            Path = "/movies/test",
            FileName = "test.movie",
            MediaHash = null,
            DateAdded = System.DateTime.UtcNow
        };
        await _dbContext.Movies.AddAsync(movie);
        await _dbContext.SaveChangesAsync();

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

        _subtitleServiceMock
            .Setup(s => s.GetAllSubtitles(It.IsAny<string>()))
            .ReturnsAsync(subtitles);

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });

        _settingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "ro", Name = "Romanian" } });

        _settingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync("false");

        // Act
        var result = await _processor.ProcessMedia(movie, MediaType.Movie);

        // Assert - Should process regardless of caption when ignoreCaptions is false
        Assert.True(result);
        _translationRequestServiceMock.Verify(
            s => s.CreateRequest(It.Is<TranslateAbleSubtitle>(t =>
                t.SourceLanguage == "en" &&
                t.TargetLanguage == "ro" &&
                t.SubtitlePath.Contains("en.forced.srt"))),
            Times.Once);
    }
}
