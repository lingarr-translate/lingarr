using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models.Integrations;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class GetShowJob
{
    private const int BatchSize = 100;

    private readonly LingarrDbContext _dbContext;
    private readonly ISonarrService _sonarrService;
    private readonly ILogger<GetShowJob> _logger;
    private readonly PathConversionService _pathConversionService;

    public GetShowJob(
        LingarrDbContext dbContext,
        ISonarrService sonarrService,
        ILogger<GetShowJob> logger, 
        PathConversionService pathConversionService)
    {
        _dbContext = dbContext;
        _sonarrService = sonarrService;
        _logger = logger;
        _pathConversionService = pathConversionService;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task Execute(IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Sonarr job initiated");
        try
        {
            var shows = await _sonarrService.GetShows();
            if (shows == null) return;

            _logger.LogInformation("Fetched {ShowCount} shows from Sonarr", shows.Count);
            
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var processedCount = 0;
            foreach (var show in shows)
            {
                var showEntity = await CreateOrUpdateShow(show);
                ProcessImages(show, showEntity);

                foreach (var season in show.Seasons)
                {
                    var seasonPath = await GetSeasonPath(show, season);
                    var seasonEntity = await CreateOrUpdateSeason(showEntity, season, seasonPath);
                    await CreateOrUpdateEpisodes(show, seasonEntity);
                }
                processedCount++;

                if (processedCount % BatchSize == 0)
                {
                    await SaveChanges(processedCount, shows.Count);
                }
            }

            // Save any remaining changes
            if (processedCount % BatchSize != 0)
            {
                await SaveChanges(processedCount, shows.Count);
            }

            // Delete shows that exist in Lingarr but not in Sonarr
            var sonarrIds = shows.Select(s => s.Id).ToHashSet();
            var showsToDelete = await _dbContext.Shows
                .Where(s => !sonarrIds.Contains(s.SonarrId))
                .ToListAsync();

            if (showsToDelete.Any())
            {
                _logger.LogInformation("Removing {Count} shows that no longer exist in Sonarr", showsToDelete.Count);
                _dbContext.Shows.RemoveRange(showsToDelete);
                await _dbContext.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Shows processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error occurred when processing shows. Exception details: {ExceptionMessage}, Stack Trace: {StackTrace}",
                ex.Message, ex.StackTrace);
        }
    }

    private async Task SaveChanges(int processedCount, int totalCount)
    {
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Processed and saved {ProcessedCount} out of {TotalCount} shows", processedCount, totalCount);
    }

    private async Task<Show> CreateOrUpdateShow(SonarrShow show)
    {
        var showEntity = await _dbContext.Shows
            .Include(s => s.Images)
            .Include(s => s.Seasons)
            .FirstOrDefaultAsync(s => s.SonarrId == show.Id);

        string showPath = show.Path;
        if (showEntity == null)
        {
            showEntity = new Show
            {
                SonarrId = show.Id,
                Title = show.Title,
                Path = showPath,
                DateAdded = !string.IsNullOrEmpty(show.Added) ? DateTime.Parse(show.Added) : DateTime.UtcNow
            };
            _dbContext.Shows.Add(showEntity);
        }
        else
        {
            showEntity.Title = show.Title;
            showEntity.Path = showPath;
            showEntity.DateAdded = !string.IsNullOrEmpty(show.Added) ? DateTime.Parse(show.Added) : DateTime.UtcNow;
        }

        return showEntity;
    }

    private async Task<Season> CreateOrUpdateSeason(Show showEntity, SonarrSeason season, string? seasonPath = null)
    {
        var seasonEntity = await _dbContext.Seasons
            .Include(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.ShowId == showEntity.Id && s.SeasonNumber == season.SeasonNumber);

        if (seasonEntity == null)
        {
            seasonEntity = new Season
            {
                SeasonNumber = season.SeasonNumber,
                Path = seasonPath ?? string.Empty,
                Show = showEntity
            };
            showEntity.Seasons.Add(seasonEntity);
        }
        else
        {
            seasonEntity.SeasonNumber = season.SeasonNumber;
            seasonEntity.Path = seasonPath ?? string.Empty;
            seasonEntity.Show = showEntity;
        }

        return seasonEntity;
    }

    private async Task CreateOrUpdateEpisodes(SonarrShow show, Season seasonEntity)
    {
        var episodes = await _sonarrService.GetEpisodes(show.Id, seasonEntity.SeasonNumber);
        if (episodes == null) return;

        foreach (var episode in episodes.Where(e => e.HasFile))
        {
            var episodePathResult = await _sonarrService.GetEpisodePath(episode.Id);
            var episodePath = _pathConversionService.ConvertAndMapPath(
                episodePathResult?.EpisodeFile.Path ?? string.Empty,
                MediaType.Show
                );
            
            var episodeEntity = seasonEntity.Episodes.FirstOrDefault(se => se.SonarrId == episode.Id);
            if (episodeEntity == null)
            {
                episodeEntity = new Episode
                {
                    SonarrId = episode.Id,
                    EpisodeNumber = episode.EpisodeNumber,
                    Title = episode.Title,
                    FileName = Path.GetFileNameWithoutExtension(episodePath),
                    Path = Path.GetDirectoryName(episodePath),
                    Season = seasonEntity
                };
                seasonEntity.Episodes.Add(episodeEntity);
            }
            else
            {
                episodeEntity.SonarrId = episode.Id;
                episodeEntity.EpisodeNumber = episode.EpisodeNumber;
                episodeEntity.Title = episode.Title;
                episodeEntity.FileName = Path.GetFileNameWithoutExtension(episodePath);
                episodeEntity.Path = Path.GetDirectoryName(episodePath);
                episodeEntity.Season = seasonEntity;
            }
        }

        // Remove episodes that no longer exist in Sonarr
        var episodesToRemove = seasonEntity.Episodes
            .Where(seasonEpisode => episodes.All(episode => episode.Id != seasonEpisode.SonarrId))
            .ToList();

        foreach (var episodeToRemove in episodesToRemove)
        {
            seasonEntity.Episodes.Remove(episodeToRemove);
        }
    }

    private void ProcessImages(SonarrShow show, Show showEntity)
    {
        if (show.Images == null || !show.Images.Any())
        {
            return;
        }

        foreach (var image in show.Images)
        {
            if (string.IsNullOrEmpty(image.CoverType) || string.IsNullOrEmpty(image.Url))
            {
                continue;
            }

            var path = image.Url.Split('?')[0];
            if (showEntity.Images.Any(m => m.Path == path))
            {
                continue;
            }

            var imageEntity = new Image
            {
                Type = image.CoverType,
                Path = path
            };

            showEntity.Images.Add(imageEntity);
        }
    }

    private async Task<string> GetSeasonPath(SonarrShow show, SonarrSeason season)
    {
        if (show.SeasonFolder)
        {
            var episodes = await _sonarrService.GetEpisodes(show.Id, season.SeasonNumber);
            var episode = episodes?.Where(episode => episode.HasFile).FirstOrDefault();
            if (episode != null)
            {
                var episodePathResult = await _sonarrService.GetEpisodePath(episode.Id);
                var seasonPath = Path.GetDirectoryName(episodePathResult?.EpisodeFile.Path);
                if (seasonPath != null)
                {
                    if (!seasonPath.StartsWith("/"))
                    {
                        seasonPath = $"/{seasonPath}";
                    }
                }
                else
                {
                    seasonPath = $"/Season {season.SeasonNumber}";
                }


                return _pathConversionService.ConvertAndMapPath(
                    seasonPath,
                    MediaType.Show
                );
            }
        }

        return string.Empty;
    }
}