using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models.Integrations;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class GetShowJob
{
    private const string LingarRootFolder = "/app/media/tv/";
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

                foreach (var season in show.seasons)
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
            .FirstOrDefaultAsync(s => s.SonarrId == show.id);

        string showPath = show.path ?? string.Empty;
        if (showEntity == null)
        {
            showEntity = new Show
            {
                SonarrId = show.id,
                Title = show.title,
                Path = showPath,
                DateAdded = !string.IsNullOrEmpty(show.added) ? DateTime.Parse(show.added) : DateTime.UtcNow
            };
            _dbContext.Shows.Add(showEntity);
        }
        else
        {
            showEntity.Title = show.title;
            showEntity.Path = showPath;
            showEntity.DateAdded = !string.IsNullOrEmpty(show.added) ? DateTime.Parse(show.added) : DateTime.UtcNow;
        }

        return showEntity;
    }

    private async Task<Season> CreateOrUpdateSeason(Show showEntity, SonarrSeason season, string? seasonPath = null)
    {
        var seasonEntity = await _dbContext.Seasons
            .Include(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.ShowId == showEntity.Id && s.SeasonNumber == season.seasonNumber);

        if (seasonEntity == null)
        {
            seasonEntity = new Season
            {
                SeasonNumber = season.seasonNumber,
                Path = seasonPath ?? string.Empty,
                Show = showEntity
            };
            showEntity.Seasons.Add(seasonEntity);
        }
        else
        {
            seasonEntity.SeasonNumber = season.seasonNumber;
            seasonEntity.Path = seasonPath ?? string.Empty;
            seasonEntity.Show = showEntity;
        }

        return seasonEntity;
    }

    private async Task CreateOrUpdateEpisodes(SonarrShow show, Season seasonEntity)
    {
        var episodes = await _sonarrService.GetEpisodes(show.id, seasonEntity.SeasonNumber);
        if (episodes == null) return;

        foreach (var episode in episodes.Where(e => e.hasFile))
        {
            var episodePathResult = await _sonarrService.GetEpisodePath(episode.id);
            var episodePath = _pathConversionService.ConvertToAppPath(
                episodePathResult?.episodeFile.path ?? string.Empty,
                LingarRootFolder);
            
            var episodeEntity = seasonEntity.Episodes.FirstOrDefault(se => se.SonarrId == episode.id);
            if (episodeEntity == null)
            {
                
                episodeEntity = new Episode
                {
                    SonarrId = episode.id,
                    EpisodeNumber = episode.episodeNumber,
                    Title = episode.title,
                    FileName = Path.GetFileNameWithoutExtension(episodePath),
                    Path = Path.GetDirectoryName(episodePath),
                    Season = seasonEntity
                };
                seasonEntity.Episodes.Add(episodeEntity);
            }
            else
            {
                episodeEntity.SonarrId = episode.id;
                episodeEntity.EpisodeNumber = episode.episodeNumber;
                episodeEntity.Title = episode.title;
                episodeEntity.FileName = Path.GetFileNameWithoutExtension(episodePath);
                episodeEntity.Path = Path.GetDirectoryName(episodePath);
                episodeEntity.Season = seasonEntity;
            }
        }

        // Remove episodes that no longer exist in Sonarr
        var episodesToRemove = seasonEntity.Episodes
            .Where(seasonEpisode => episodes.All(episode => episode.id != seasonEpisode.SonarrId))
            .ToList();

        foreach (var episodeToRemove in episodesToRemove)
        {
            seasonEntity.Episodes.Remove(episodeToRemove);
        }
    }

    private void ProcessImages(SonarrShow show, Show showEntity)
    {
        foreach (var image in show.images)
        {
            if (string.IsNullOrEmpty(image.coverType) || string.IsNullOrEmpty(image.url))
            {
                continue;
            }

            var path = image.url.Split('?')[0];
            if (showEntity.Images.Any(m => m.Path == path))
            {
                continue;
            }

            var imageEntity = new Image
            {
                Type = image.coverType,
                Path = path
            };

            showEntity.Images.Add(imageEntity);
        }
    }

    private async Task<string> GetSeasonPath(SonarrShow show, SonarrSeason season)
    {
        if (show.seasonFolder)
        {
            var episodes = await _sonarrService.GetEpisodes(show.id, season.seasonNumber);
            var episode = episodes?.Where(episode => episode.hasFile).FirstOrDefault();
            if (episode != null)
            {
                var episodePathResult = await _sonarrService.GetEpisodePath(episode.id);
                var seasonPath = Path.GetDirectoryName(episodePathResult?.episodeFile.path);
                if (seasonPath != null)
                {
                    if (!seasonPath.StartsWith("/"))
                    {
                        seasonPath = $"/{seasonPath}";
                    }
                }
                else
                {
                    seasonPath = $"/Season {season.seasonNumber}";
                }

                
                return _pathConversionService.ConvertToAppPath(seasonPath, LingarRootFolder);
            }
        }

        return string.Empty;
    }
}