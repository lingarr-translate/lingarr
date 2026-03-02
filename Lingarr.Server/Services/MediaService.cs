using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Microsoft.EntityFrameworkCore;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Interfaces.Services.Integration;

namespace Lingarr.Server.Services;

public class MediaService : IMediaService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISubtitleService _subtitleService;
    private readonly ISonarrService _sonarrService;
    private readonly IShowSyncService _showSyncService;
    private readonly IRadarrService _radarrService;
    private readonly IMovieSyncService _movieSyncService;
    private readonly ILogger<MediaService> _logger;

    public MediaService(LingarrDbContext dbContext, 
        ISubtitleService subtitleService,
        ISonarrService sonarrService,
        IShowSyncService showSyncService,
        IRadarrService radarrService,
        IMovieSyncService movieSyncService,
        ILogger<MediaService> logger)
    {
        _dbContext = dbContext;
        _subtitleService = subtitleService;
        _sonarrService = sonarrService;
        _showSyncService = showSyncService;
        _radarrService = radarrService;
        _movieSyncService = movieSyncService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<PagedResult<MovieResponse>> GetMovies(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.Movies
            .Include(m => m.Images)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(m => m.Title.ToLower().Contains(searchQuery.ToLower()));
        }

        query = orderBy switch
        {
            "Id" => ascending ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id),
            "Title" => ascending ? query.OrderBy(m => m.Title) : query.OrderByDescending(m => m.Title),
            "DateAdded" => ascending ? query.OrderBy(m => m.DateAdded) : query.OrderByDescending(m => m.DateAdded),
            _ => ascending ? query.OrderBy(m => m.Title) : query.OrderByDescending(m => m.Title)
        };

        var totalCount = await query.CountAsync();
        var movies = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var enrichedMovies = new List<MovieResponse>();
        foreach (var movie in movies)
        {
            if (movie.Path == null)
            {
                continue;
            }

            var subtitles = await _subtitleService.GetAllSubtitles(movie.Path);
            var enrichedMovie = new MovieResponse
            {
                Id = movie.Id,
                RadarrId = movie.RadarrId,
                Title = movie.Title,
                FileName = movie.FileName ?? string.Empty,
                Path = movie.Path,
                DateAdded = movie.DateAdded,
                Images = movie.Images,
                Subtitles = subtitles,
                IncludeInTranslation = movie.IncludeInTranslation,
                TranslationAgeThreshold = movie.TranslationAgeThreshold
            };
            enrichedMovies.Add(enrichedMovie);
        }

        return new PagedResult<MovieResponse>
        {
            Items = enrichedMovies,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<int> GetMovieIdOrSyncFromRadarrMovieId(int movieId)
    {
        var movie = await _dbContext.Movies.Where(s => s.RadarrId == movieId).FirstOrDefaultAsync();
        if (movie != null)
        {
            return movie.Id;
        }

        // Movie not found, maybe out of sync.
        // Sync the movie
        try
        {
            var movieFetched = await _radarrService.GetMovie(movieId);
            if (movieFetched == null)
            {
                // Unknown movie
                return 0;
            }

            var movieEntity = await _movieSyncService.SyncMovie(movieFetched);
            if (movieEntity == null)
            {
                // Movie had no file
                return 0;
            }
            
            return movieEntity.Id;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Movie doesn't exist in Radarr
            _logger.LogWarning("Movie with Radarr ID {MovieId} not found in Radarr (404)", movieId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or sync movie with Radarr ID {MovieId}", movieId);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetEpisodeIdOrSyncFromSonarrEpisodeId(int episodeNumber)
    {
        var episode = await _dbContext.Episodes.Where(s => s.SonarrId == episodeNumber).FirstOrDefaultAsync();
        if (episode != null)
        {
            return episode.Id;
        }

        // Episode not found, maybe out of sync.
        // Sync the show
        try
        {
            var episodeFetched = await _sonarrService.GetEpisode(episodeNumber);
            if (episodeFetched == null)
            {
                // Unknown episode
                return 0;
            }

            if (episodeFetched.Show == null)
            {
                // Show not found with episode
                return 0;
            }

            var show = await _showSyncService.SyncShow(episodeFetched.Show);
            // Find the episode id or return 0 if not found
            return show.Seasons
                .SelectMany(s => s.Episodes)
                .FirstOrDefault(e => e.SonarrId == episodeNumber)?.Id ?? 0;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Episode doesn't exist in Sonarr
            _logger.LogWarning("Episode with Sonarr ID {EpisodeId} not found in Sonarr (404)", episodeNumber);
            try
            {
                // Attempt a more comprehensive resync of all shows in case the Sonarr ID is stale
                _logger.LogInformation("Sonarr episode {EpisodeId} returned 404 — attempting full show resync as a fallback.", episodeNumber);
                var shows = await _sonarrService.GetShows();
                if (shows != null && shows.Any())
                {
                    await _showSyncService.SyncShows(shows);
                    // Try to find the episode again after resync
                    var matchedEpisode = await _dbContext.Episodes.Where(s => s.SonarrId == episodeNumber).FirstOrDefaultAsync();
                    if (matchedEpisode != null)
                    {
                        return matchedEpisode.Id;
                    }
                }
            }
            catch (Exception syncEx)
            {
                _logger.LogWarning(syncEx, "Resync attempt after Sonarr 404 failed for episode {EpisodeId}", episodeNumber);
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or sync episode with Sonarr ID {EpisodeId}", episodeNumber);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<Show>> GetShows(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.Shows
            .Include(s => s.Images)
            .Include(s => s.Seasons)
            .ThenInclude(season => season.Episodes)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(s => s.Title.ToLower().Contains(searchQuery.ToLower()));
        }

        query = orderBy switch
        {
            "Id" => ascending ? query.OrderBy(s => s.Id) : query.OrderByDescending(s => s.Id),
            "Title" => ascending ? query.OrderBy(s => s.Title) : query.OrderByDescending(s => s.Title),
            "DateAdded" => ascending ? query.OrderBy(s => s.DateAdded) : query.OrderByDescending(s => s.DateAdded),
            _ => ascending ? query.OrderBy(s => s.Title) : query.OrderByDescending(s => s.Title)
        };

        var totalCount = await query.CountAsync();
        var shows = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Show>
        {
            Items = shows,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    
    /// <inheritdoc />
    public async Task<bool> Exclude(
        MediaType mediaType,
        int id)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    var movie = await _dbContext.Movies.FindAsync(id);
                    if (movie != null)
                    {
                        movie.IncludeInTranslation = !movie.IncludeInTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Show:
                    var show = await _dbContext.Shows.FindAsync(id);
                    if (show != null)
                    {
                        show.IncludeInTranslation = !show.IncludeInTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Season:
                    var season = await _dbContext.Seasons.FindAsync(id);
                    if (season != null)
                    {
                        season.IncludeInTranslation = !season.IncludeInTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Episode:
                    var episode = await _dbContext.Episodes.FindAsync(id);
                    if (episode != null)
                    {
                        episode.IncludeInTranslation = !episode.IncludeInTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                default:
                    _logger.LogWarning("Unsupported media type: {MediaType}", mediaType);
                    return false;
            }

            _logger.LogWarning("Media item not found. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error excluding media item. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> SetInclude(
        MediaType mediaType,
        int id,
        bool include)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    var movieCount = await _dbContext.Movies
                        .Where(m => m.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(m => m.IncludeInTranslation, include));
                    if (movieCount > 0) return true;
                    break;

                case MediaType.Show:
                    var showCount = await _dbContext.Shows
                        .Where(s => s.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(sh => sh.IncludeInTranslation, include));
                    if (showCount > 0) return true;
                    break;

                case MediaType.Season:
                    var seasonCount = await _dbContext.Seasons
                        .Where(s => s.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(se => se.IncludeInTranslation, include));
                    if (seasonCount > 0) return true;
                    break;

                case MediaType.Episode:
                    var episodeCount = await _dbContext.Episodes
                        .Where(e => e.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(ep => ep.IncludeInTranslation, include));
                    if (episodeCount > 0) return true;
                    break;

                default:
                    _logger.LogWarning("Unsupported media type for SetInclude: {MediaType}", mediaType);
                    return false;
            }

            _logger.LogWarning("Media item not found for SetInclude. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting include for media item. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetIncludeAll(
        MediaType mediaType,
        bool include)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    await using (var movieTransaction = await _dbContext.Database.BeginTransactionAsync())
                    {
                        await _dbContext.Movies
                            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IncludeInTranslation, include));
                        await movieTransaction.CommitAsync();
                    }
                    return true;

                case MediaType.Show:
                    await using (var showTransaction = await _dbContext.Database.BeginTransactionAsync())
                    {
                        // Update all shows
                        await _dbContext.Shows
                            .ExecuteUpdateAsync(s => s.SetProperty(sh => sh.IncludeInTranslation, include));

                        // Cascade to all seasons that belong to existing shows, without materializing IDs
                        await _dbContext.Seasons
                            .Where(se => _dbContext.Shows.Any(sh => sh.Id == se.ShowId))
                            .ExecuteUpdateAsync(s => s.SetProperty(se => se.IncludeInTranslation, include));

                        // Cascade to all episodes that belong to seasons of existing shows, without materializing IDs
                        await _dbContext.Episodes
                            .Where(ep => _dbContext.Seasons
                                .Any(se => se.Id == ep.SeasonId &&
                                           _dbContext.Shows.Any(sh => sh.Id == se.ShowId)))
                            .ExecuteUpdateAsync(s => s.SetProperty(ep => ep.IncludeInTranslation, include));

                        await showTransaction.CommitAsync();
                    }
                    return true;

                default:
                    _logger.LogWarning("Unsupported media type for SetIncludeAll: {MediaType}. Use Movie or Show.", mediaType);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting include all for media type: {MediaType}", mediaType);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IncludeSummary> GetIncludeSummary(MediaType mediaType)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                {
                    var movieCounts = await _dbContext.Movies
                        .GroupBy(m => 1)
                        .Select(g => new
                        {
                            Included = g.Count(m => m.IncludeInTranslation),
                            Total = g.Count()
                        })
                        .FirstOrDefaultAsync();

                    return new IncludeSummary
                    {
                        Included = movieCounts?.Included ?? 0,
                        Total = movieCounts?.Total ?? 0
                    };
                }

                case MediaType.Show:
                {
                    var showCounts = await _dbContext.Shows
                        .GroupBy(s => 1)
                        .Select(g => new
                        {
                            Included = g.Count(s => s.IncludeInTranslation),
                            Total = g.Count()
                        })
                        .FirstOrDefaultAsync();

                    return new IncludeSummary
                    {
                        Included = showCounts?.Included ?? 0,
                        Total = showCounts?.Total ?? 0
                    };
                }

                default:
                    _logger.LogWarning("Unsupported media type for GetIncludeSummary: {MediaType}", mediaType);
                    return new IncludeSummary { Included = 0, Total = 0 };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting include summary for media type: {MediaType}", mediaType);
            return new IncludeSummary { Included = 0, Total = 0 };
        }
    }

    /// <inheritdoc />
    public async Task<bool> Threshold(
        MediaType mediaType,
        int id,
        int hours)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    var movie = await _dbContext.Movies.FindAsync(id);
                    if (movie != null)
                    {
                        movie.TranslationAgeThreshold = hours;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Show:
                    var show = await _dbContext.Shows.FindAsync(id);
                    if (show != null)
                    {
                        show.TranslationAgeThreshold = hours;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;
                
                default:
                    _logger.LogWarning("Unsupported media type: {MediaType}", mediaType);
                    return false;
            }

            _logger.LogWarning("Media item not found. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error excluding media item. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
    }
}