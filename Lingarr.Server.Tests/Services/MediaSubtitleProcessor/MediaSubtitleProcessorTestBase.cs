using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lingarr.Server.Tests.Services.MediaSubtitleProcessor;

/// <summary>
/// Base test class providing common setup for MediaSubtitleProcessor tests.
/// </summary>
public abstract class MediaSubtitleProcessorTestBase : IDisposable
{
    protected readonly Mock<ITranslationRequestService> TranslationRequestServiceMock;
    protected readonly Mock<ILogger<IMediaSubtitleProcessor>> LoggerMock;
    protected readonly Mock<ISubtitleService> SubtitleServiceMock;
    protected readonly Mock<ISettingService> SettingServiceMock;
    protected readonly Mock<ISubtitleExtractionService> SubtitleExtractionServiceMock;
    protected readonly LingarrDbContext DbContext;
    protected readonly Lingarr.Server.Services.MediaSubtitleProcessor Processor;

    protected MediaSubtitleProcessorTestBase()
    {
        TranslationRequestServiceMock = new Mock<ITranslationRequestService>();
        LoggerMock = new Mock<ILogger<IMediaSubtitleProcessor>>();
        SubtitleServiceMock = new Mock<ISubtitleService>();
        SettingServiceMock = new Mock<ISettingService>();
        SubtitleExtractionServiceMock = new Mock<ISubtitleExtractionService>();

        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        DbContext = new LingarrDbContext(options);

        var subtitleService = new SubtitleService(
            new Mock<ILogger<SubtitleService>>().Object,
            new LanguageCodeService());
        SubtitleServiceMock
            .Setup(s => s.SelectSourceSubtitle(
                It.IsAny<List<Subtitles>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns((List<Subtitles> subs, IReadOnlyList<string> sources) =>
                subtitleService.SelectSourceSubtitle(subs, sources));
        SubtitleServiceMock
            .Setup(s => s.TargetSubtitleExists(
                It.IsAny<List<Subtitles>>(),
                It.IsAny<string>(),
                It.IsAny<Subtitles>(),
                It.IsAny<bool>()))
            .Returns((List<Subtitles> subs, string target, Subtitles source, bool ignoreCaptions) =>
                subtitleService.TargetSubtitleExists(subs, target, source, ignoreCaptions));

        Processor = new Lingarr.Server.Services.MediaSubtitleProcessor(
            TranslationRequestServiceMock.Object,
            LoggerMock.Object,
            SettingServiceMock.Object,
            SubtitleServiceMock.Object,
            SubtitleExtractionServiceMock.Object,
            DbContext);
    }

    protected async Task<Movie> CreateTestMovie(string fileName = "test.movie")
    {
        var movie = new Movie
        {
            Id = 1,
            RadarrId = 1,
            Title = "Test Movie",
            Path = "/movies/test",
            FileName = fileName,
            MediaHash = null,
            DateAdded = System.DateTime.UtcNow
        };
        await DbContext.Movies.AddAsync(movie);
        await DbContext.SaveChangesAsync();
        return movie;
    }

    protected void SetupStandardSettings(string ignoreCaptions = "true")
    {
        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<SourceLanguage>(SettingKeys.Translation.SourceLanguages))
            .ReturnsAsync(new List<SourceLanguage> { new() { Code = "en", Name = "English" } });

        SettingServiceMock
            .Setup(s => s.GetSettingAsJson<TargetLanguage>(SettingKeys.Translation.TargetLanguages))
            .ReturnsAsync(new List<TargetLanguage> { new() { Code = "ro", Name = "Romanian" } });

        SettingServiceMock
            .Setup(s => s.GetSetting(SettingKeys.Translation.IgnoreCaptions))
            .ReturnsAsync(ignoreCaptions);
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
