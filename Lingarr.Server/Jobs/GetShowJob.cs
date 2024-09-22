using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class GetShowJob
{
    private const string LingarRootFolder = "/tv/";
    
    private readonly LingarrDbContext _dbContext;
    private readonly ISonarrService _sonarrService;
    private readonly ILogger<GetShowJob> _logger;

    public GetShowJob(
        LingarrDbContext dbContext,
        ISonarrService sonarrService,
        ILogger<GetShowJob> logger)
    {
        _dbContext = dbContext;
        _sonarrService = sonarrService;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task Execute(IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Sonarr job initiated");
        try
        {
            var shows = await _sonarrService.GetShows();
            if (shows == null) return;

            _logger.LogInformation("Fetched {ShowCount} shows from Sonarr", shows.Count);

            foreach (var show in shows)
            {
                await ProcessShow(show);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Shows processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when processing shows");
        }
    }

    private async Task ProcessShow(SonarrShow show)
    {
        var showEntity = await CreateOrUpdateShow(show);
        await ProcessImages(show, showEntity);
        await ProcessSeasons(show, showEntity);
    }

    private async Task<Show> CreateOrUpdateShow(SonarrShow show)
    {
        var showEntity = await _dbContext.Shows
            .Include(s => s.Media)
            .Include(s => s.Seasons)
            .FirstOrDefaultAsync(s => s.SonarrId == show.id);

        if (showEntity == null)
        {
            showEntity = new Show
            {
                SonarrId = show.id,
                Title = show.title,
                Path =  GetPath(show.path, show.rootFolderPath),
                DateAdded = !string.IsNullOrEmpty(show.added) ? DateTime.Parse(show.added) : DateTime.UtcNow
            };
            _dbContext.Shows.Add(showEntity);
        }
        else
        {
            showEntity.Title = show.title;
            showEntity.Path = GetPath(show.path, show.rootFolderPath);
            showEntity.DateAdded = !string.IsNullOrEmpty(show.added) ? DateTime.Parse(show.added) : DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return showEntity;
    }

    private async Task ProcessImages(SonarrShow show, Show showEntity)
    {
        foreach (var image in show.images)
        {
            if (string.IsNullOrEmpty(image.coverType) || string.IsNullOrEmpty(image.url))
            {
                continue;
            }

            var path = image.url.Split('?')[0];
            if (showEntity.Media.Any(m => m.Path == path))
            {
                continue;
            }

            var imageEntity = new Media
            {
                Type = image.coverType,
                Path = path
            };

            showEntity.Media.Add(imageEntity);
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task ProcessSeasons(SonarrShow show, Show showEntity)
    {
        foreach (var season in show.seasons)
        {
            var seasonEntity = await CreateOrUpdateSeason(showEntity, season);
            await CreateOrUpdateEpisodes(show, season, seasonEntity);
        }
    }

    private async Task<Season> CreateOrUpdateSeason(Show showEntity, SonarrSeason season)
    {
        var seasonEntity = await _dbContext.Seasons
            .Include(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.ShowId == showEntity.Id && s.SeasonNumber == season.seasonNumber);

        if (seasonEntity == null)
        {
            seasonEntity = new Season
            {
                SeasonNumber = season.seasonNumber,
                Path = $"{showEntity.Path}/Season {season.seasonNumber}",
                Show = showEntity
            };
            showEntity.Seasons.Add(seasonEntity);
        }
        else
        {
            seasonEntity.Path = $"{showEntity.Path}/Season {season.seasonNumber}";
            seasonEntity.SeasonNumber = season.seasonNumber;
            seasonEntity.Show = showEntity;
        }

        return seasonEntity;
    }

    private async Task CreateOrUpdateEpisodes(SonarrShow show, SonarrSeason season, Season seasonEntity)
    {
        var episodes = await _sonarrService.GetEpisodes(show.id, season.seasonNumber);
        if (episodes == null) return;
    
        foreach (var episode in episodes.Where(e => e.hasFile))
        {
            var episodeEntity = seasonEntity.Episodes.FirstOrDefault(se => se.SonarrId == episode.id);
            var episodePathResult = await _sonarrService.GetEpisodePath(episode.id);

            string episodePath = GetPath(episodePathResult?.episodeFile.path, show.rootFolderPath);
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

    private string GetPath(string? path, string rootFolderPath)
    {
        if (path != null &&
            path.StartsWith(rootFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return LingarRootFolder + path.Substring(rootFolderPath.Length);
        }

        return path ?? string.Empty;
    }
}