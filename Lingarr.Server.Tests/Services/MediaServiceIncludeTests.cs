using System;
using System.Linq;
using System.Threading.Tasks;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class MediaServiceIncludeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LingarrDbContext _context;
    private readonly MediaService _mediaService;

    public MediaServiceIncludeTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LingarrDbContext(options);
        _context.Database.EnsureCreated();

        _mediaService = new MediaService(
            _context,
            new Mock<ISubtitleService>().Object,
            new Mock<ISonarrService>().Object,
            new Mock<IShowSyncService>().Object,
            new Mock<IRadarrService>().Object,
            new Mock<IMovieSyncService>().Object,
            NullLogger<MediaService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Movie CreateMovie(int radarrId, string title, bool exclude = true) => new()
    {
        RadarrId = radarrId,
        Title = title,
        FileName = null,
        Path = null,
        DateAdded = DateTime.UtcNow,
        IncludeInTranslation = !exclude
    };

    private static Show CreateShow(int sonarrId, string title, bool exclude = true) => new()
    {
        SonarrId = sonarrId,
        Title = title,
        Path = "/tmp",
        DateAdded = DateTime.UtcNow,
        IncludeInTranslation = !exclude
    };

    private static Season CreateSeason(int seasonNumber, Show show, bool exclude = true) => new()
    {
        SeasonNumber = seasonNumber,
        Show = show,
        IncludeInTranslation = !exclude
    };

    private static Episode CreateEpisode(int sonarrId, int episodeNumber, string title, Season season, bool exclude = true) => new()
    {
        SonarrId = sonarrId,
        EpisodeNumber = episodeNumber,
        Title = title,
        Season = season,
        IncludeInTranslation = !exclude
    };

    #region SetInclude Tests

    [Fact]
    public async Task SetInclude_Movie_SetsIncludeToTrue()
    {
        var movie = CreateMovie(1, "Test Movie", exclude: true);
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetInclude(MediaType.Movie, movie.Id, true);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var updated = await _context.Movies.FindAsync(movie.Id);
        Assert.True(updated!.IncludeInTranslation);
    }

    [Fact]
    public async Task SetInclude_Movie_SetsIncludeToFalse()
    {
        var movie = CreateMovie(2, "Test Movie 2", exclude: false);
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetInclude(MediaType.Movie, movie.Id, false);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var updated = await _context.Movies.FindAsync(movie.Id);
        Assert.False(updated!.IncludeInTranslation);
    }

    [Fact]
    public async Task SetInclude_Show_SetsIncludeToTrue()
    {
        var show = CreateShow(1, "Test Show", exclude: true);
        _context.Shows.Add(show);
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetInclude(MediaType.Show, show.Id, true);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var updated = await _context.Shows.FindAsync(show.Id);
        Assert.True(updated!.IncludeInTranslation);
    }

    [Fact]
    public async Task SetInclude_Season_SetsIncludeToTrue()
    {
        var show = CreateShow(2, "Show for Season");
        _context.Shows.Add(show);
        await _context.SaveChangesAsync();
        var season = CreateSeason(1, show, exclude: true);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetInclude(MediaType.Season, season.Id, true);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var updated = await _context.Seasons.FindAsync(season.Id);
        Assert.True(updated!.IncludeInTranslation);
    }

    [Fact]
    public async Task SetInclude_Episode_SetsIncludeToTrue()
    {
        var show = CreateShow(3, "Show for Episode");
        _context.Shows.Add(show);
        await _context.SaveChangesAsync();
        var season = CreateSeason(1, show);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();
        var episode = CreateEpisode(1, 1, "Ep 1", season, exclude: true);
        _context.Episodes.Add(episode);
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetInclude(MediaType.Episode, episode.Id, true);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var updated = await _context.Episodes.FindAsync(episode.Id);
        Assert.True(updated!.IncludeInTranslation);
    }

    [Fact]
    public async Task SetInclude_NonExistentMovie_ReturnsFalse()
    {
        var result = await _mediaService.SetInclude(MediaType.Movie, 9999, true);
        Assert.False(result);
    }

    #endregion

    #region SetIncludeAll Tests

    [Fact]
    public async Task SetIncludeAll_Movies_IncludesAll()
    {
        _context.Movies.AddRange(
            CreateMovie(10, "Movie A", exclude: true),
            CreateMovie(11, "Movie B", exclude: true),
            CreateMovie(12, "Movie C", exclude: false)
        );
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetIncludeAll(MediaType.Movie, true);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var movies = await _context.Movies.ToListAsync();
        Assert.All(movies, m => Assert.True(m.IncludeInTranslation));
    }

    [Fact]
    public async Task SetIncludeAll_Movies_SetsAllToFalse()
    {
        _context.Movies.AddRange(
            CreateMovie(20, "Movie X", exclude: false),
            CreateMovie(21, "Movie Y", exclude: false)
        );
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetIncludeAll(MediaType.Movie, false);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        var movies = await _context.Movies.ToListAsync();
        Assert.All(movies, m => Assert.False(m.IncludeInTranslation));
    }

    [Fact]
    public async Task SetIncludeAll_Shows_CascadesToSeasonsAndEpisodes()
    {
        var show = CreateShow(100, "Cascade Show", exclude: true);
        _context.Shows.Add(show);
        await _context.SaveChangesAsync();

        var season1 = CreateSeason(1, show, exclude: true);
        var season2 = CreateSeason(2, show, exclude: true);
        _context.Seasons.AddRange(season1, season2);
        await _context.SaveChangesAsync();

        _context.Episodes.AddRange(
            CreateEpisode(101, 1, "Ep 1", season1, exclude: true),
            CreateEpisode(102, 2, "Ep 2", season1, exclude: true),
            CreateEpisode(103, 1, "Ep 1 S2", season2, exclude: true)
        );
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetIncludeAll(MediaType.Show, true);

        Assert.True(result);
        _context.ChangeTracker.Clear();

        var updatedShow = await _context.Shows.FindAsync(show.Id);
        Assert.True(updatedShow!.IncludeInTranslation);

        var seasons = await _context.Seasons.Where(s => s.ShowId == show.Id).ToListAsync();
        Assert.All(seasons, s => Assert.True(s.IncludeInTranslation));

        var episodes = await _context.Episodes.ToListAsync();
        Assert.All(episodes, e => Assert.True(e.IncludeInTranslation));
    }

    [Fact]
    public async Task SetIncludeAll_Shows_ExcludeCascades()
    {
        var show = CreateShow(200, "Exclude Show", exclude: false);
        _context.Shows.Add(show);
        await _context.SaveChangesAsync();

        var season = CreateSeason(1, show, exclude: false);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var ep = CreateEpisode(201, 1, "Ep 1", season, exclude: false);
        _context.Episodes.Add(ep);
        await _context.SaveChangesAsync();

        var result = await _mediaService.SetIncludeAll(MediaType.Show, false);

        Assert.True(result);
        _context.ChangeTracker.Clear();

        var updatedShow = await _context.Shows.FindAsync(show.Id);
        Assert.False(updatedShow!.IncludeInTranslation);

        var updatedSeason = await _context.Seasons.FindAsync(season.Id);
        Assert.False(updatedSeason!.IncludeInTranslation);

        var updatedEp = await _context.Episodes.FindAsync(ep.Id);
        Assert.False(updatedEp!.IncludeInTranslation);
    }

    #endregion

    #region GetIncludeSummary Tests

    [Fact]
    public async Task GetIncludeSummary_Movies_ReturnsCorrectCounts()
    {
        _context.Movies.AddRange(
            CreateMovie(30, "Incl 1", exclude: false),
            CreateMovie(31, "Incl 2", exclude: false),
            CreateMovie(32, "Excl 1", exclude: true)
        );
        await _context.SaveChangesAsync();

        var summary = await _mediaService.GetIncludeSummary(MediaType.Movie);

        Assert.Equal(2, summary.Included);
        Assert.Equal(3, summary.Total);
    }

    [Fact]
    public async Task GetIncludeSummary_Shows_ReturnsCorrectCounts()
    {
        _context.Shows.AddRange(
            CreateShow(300, "Show Incl", exclude: false),
            CreateShow(301, "Show Excl", exclude: true)
        );
        await _context.SaveChangesAsync();

        var summary = await _mediaService.GetIncludeSummary(MediaType.Show);

        Assert.Equal(1, summary.Included);
        Assert.Equal(2, summary.Total);
    }

    [Fact]
    public async Task GetIncludeSummary_EmptyDatabase_ReturnsZeros()
    {
        var summary = await _mediaService.GetIncludeSummary(MediaType.Movie);

        Assert.Equal(0, summary.Included);
        Assert.Equal(0, summary.Total);
    }

    [Fact]
    public async Task GetIncludeSummary_UnsupportedType_ReturnsZeros()
    {
        var summary = await _mediaService.GetIncludeSummary(MediaType.Season);

        Assert.Equal(0, summary.Included);
        Assert.Equal(0, summary.Total);
    }

    #endregion
}
