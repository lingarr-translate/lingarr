using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services.Sync;

public class ShowSyncService : IShowSyncService
{
    private const int BatchSize = 100;
    
    private readonly LingarrDbContext _dbContext;
    private readonly IShowSync _showSync;
    private readonly ISeasonSync _seasonSync;
    private readonly IEpisodeSync _episodeSync;
    private readonly ILogger<ShowSyncService> _logger;

    public ShowSyncService(
        LingarrDbContext dbContext,
        IShowSync showSync,
        ISeasonSync seasonSync,
        IEpisodeSync episodeSync,
        ILogger<ShowSyncService> logger)
    {
        _dbContext = dbContext;
        _showSync = showSync;
        _seasonSync = seasonSync;
        _episodeSync = episodeSync;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SyncShows(List<SonarrShow> shows)
    {
        var processedCount = 0;
        
        foreach (var show in shows)
        {
            var showEntity = await _showSync.SyncShow(show);

            foreach (var season in show.Seasons)
            {
                var seasonEntity = await _seasonSync.SyncSeason(showEntity, show, season);
                await _episodeSync.SyncEpisodes(show, seasonEntity);
            }
            
            processedCount++;

            if (processedCount % BatchSize == 0)
            {
                await SaveChanges(processedCount, shows.Count);
            }
        }

        if (processedCount % BatchSize != 0)
        {
            await SaveChanges(processedCount, shows.Count);
        }
    }

    /// <inheritdoc />
    public async Task RemoveNonExistentShows(HashSet<int> existingSonarrIds)
    {
        var showsToDelete = await _dbContext.Shows
            .Include(s => s.Images)
            .Include(s => s.Seasons)
                .ThenInclude(s => s.Episodes)
            .Where(s => !existingSonarrIds.Contains(s.SonarrId))
            .ToListAsync();

        if (showsToDelete.Any())
        {
            _logger.LogInformation("Removing {Count} shows that no longer exist in Sonarr", showsToDelete.Count);
            
            var episodes = showsToDelete.SelectMany(s => s.Seasons.SelectMany(season => season.Episodes)).ToList();
            var seasons = showsToDelete.SelectMany(s => s.Seasons).ToList();
            var images = showsToDelete.SelectMany(s => s.Images).ToList();

            _dbContext.Episodes.RemoveRange(episodes);
            _dbContext.Seasons.RemoveRange(seasons);
            _dbContext.Images.RemoveRange(images);
            _dbContext.Shows.RemoveRange(showsToDelete);
            
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Saves pending changes to the database and logs the sync progress
    /// </summary>
    /// <param name="processedCount">The number of shows processed so far</param>
    /// <param name="totalCount">The total number of shows to process</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task SaveChanges(int processedCount, int totalCount)
    {
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Synced and saved {ProcessedCount} out of {TotalCount} shows", 
            processedCount, totalCount);
    }
}