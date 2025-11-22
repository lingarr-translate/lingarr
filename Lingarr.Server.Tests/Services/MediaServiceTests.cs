using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using System.Net.Http;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class MediaServiceTests
{
    [Fact]
    public async Task GetEpisodeIdOrSyncFromSonarrEpisodeId_WhenEpisodeNotFound_TriesToResyncShows()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new LingarrDbContext(options);

        // Seed DB with no episodes
        await context.SaveChangesAsync();

        var sonarrMock = new Mock<ISonarrService>();
        // var showSyncServiceMock = new Mock<IShowSyncService>(); (already declared below)
        var radarrMock = new Mock<IRadarrService>();
        var movieSyncMock = new Mock<IMovieSyncService>();
        var subtitleMock = new Mock<ISubtitleService>();
        var logger = NullLogger<MediaService>.Instance;

        // Configure Sonarr to throw HttpRequestException with 404 status
        sonarrMock
            .Setup(s => s.GetEpisode(It.IsAny<int>()))
            .ThrowsAsync(new HttpRequestException("Not found", null, HttpStatusCode.NotFound));

        // Configure GetShows to return a sample list when called
        sonarrMock
            .Setup(s => s.GetShows())
            .ReturnsAsync(new List<Lingarr.Server.Models.Integrations.SonarrShow>()
            {
                new Lingarr.Server.Models.Integrations.SonarrShow
                {
                    Id = 1,
                    Title = "Sample Show",
                    Path = "/tmp",
                    Added = System.DateTime.UtcNow.ToString("o"),
                    SeasonFolder = false,
                    Seasons = new List<Lingarr.Server.Models.Integrations.SonarrSeason>()
                }
            });

        var showSyncServiceMock = new Mock<IShowSyncService>();

        var mediaService = new MediaService(context,
            subtitleMock.Object,
            sonarrMock.Object,
            showSyncServiceMock.Object,
            radarrMock.Object,
            movieSyncMock.Object,
            logger);

        // Act
        var result = await mediaService.GetEpisodeIdOrSyncFromSonarrEpisodeId(150);

        // Assert
        sonarrMock.Verify(s => s.GetEpisode(150), Times.Once);
        sonarrMock.Verify(s => s.GetShows(), Times.Once);
        showSyncServiceMock.Verify(s => s.SyncShows(It.IsAny<List<Lingarr.Server.Models.Integrations.SonarrShow>>()), Times.Once);
        Assert.Equal(0, result);
    }
}
