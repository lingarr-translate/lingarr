using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services.Sync;

public class MovieSyncService : IMovieSyncService
{
    private const int BatchSize = 100;
    private readonly LingarrDbContext _dbContext;
    private readonly IMovieSync _movieSync;
    private readonly ILogger<MovieSyncService> _logger;

    public MovieSyncService(
        LingarrDbContext dbContext,
        IMovieSync movieSync,
        ILogger<MovieSyncService> logger)
    {
        _dbContext = dbContext;
        _movieSync = movieSync;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SyncMovies(List<RadarrMovie> movies)
    {
        var processedCount = 0;
        
        foreach (var movie in movies)
        {
            await _movieSync.SyncMovie(movie);
            processedCount++;

            if (processedCount % BatchSize == 0)
            {
                await SaveChanges(processedCount, movies.Count);
            }
        }

        if (processedCount % BatchSize != 0)
        {
            await SaveChanges(processedCount, movies.Count);
        }
    }

    /// <inheritdoc />
    public async Task<Movie?> SyncMovie(RadarrMovie movie)
    {
        var movieEntity = await _movieSync.SyncMovie(movie);
        await _dbContext.SaveChangesAsync();

        if (movieEntity != null)
        {
            _logger.LogInformation("Synced a single movie");
        }

        return movieEntity;
    }

    /// <inheritdoc />
    public async Task RemoveNonExistentMovies(IEnumerable<int> existingRadarrIds)
    {
        var moviesToRemove = await _dbContext.Movies
            .Include(m => m.Images)
            .Where(movie => !existingRadarrIds.Contains(movie.RadarrId))
            .ToListAsync();

        if (moviesToRemove.Any())
        {
            _logger.LogInformation("Removing {Count} movies that no longer exist in Radarr", moviesToRemove.Count);

            // Bulk remove all images and movies
            var imagesToRemove = moviesToRemove.SelectMany(m => m.Images).ToList();
            _dbContext.Images.RemoveRange(imagesToRemove);
            _dbContext.Movies.RemoveRange(moviesToRemove);

            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Saves pending changes to the database and logs the sync progress
    /// </summary>
    /// <param name="processedCount">The number of movies processed so far</param>
    /// <param name="totalCount">The total number of movies to process</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task SaveChanges(int processedCount, int totalCount)
    {
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Synced and saved {ProcessedCount} out of {TotalCount} movies", 
            processedCount, totalCount);
    }
}