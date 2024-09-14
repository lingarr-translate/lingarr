using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Models.Integrations;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class GetShowJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly SonarrService _sonarrService;
    private readonly ILogger<GetShowJob> _logger;

    public GetShowJob(
        LingarrDbContext dbContext,
        SonarrService sonarrService,
        ILogger<GetShowJob> logger)
    {
        _dbContext = dbContext;
        _sonarrService = sonarrService;
        _logger = logger;
    }

    public async Task Execute()
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
        var showEntity = await GetOrCreateShow(show);
        await ProcessImages(show, showEntity);
        await ProcessSeasons(show, showEntity);
    }

    private async Task<Show> GetOrCreateShow(SonarrShow show)
    {
        var showEntity = await _dbContext.Shows
            .Include(s => s.Media)
            .Include(s => s.Seasons)
            .FirstOrDefaultAsync(s => s.SonarrId == show.id);
        if (showEntity != null)
        {
            return showEntity;
        }

        showEntity = new Show
        {
            SonarrId = show.id,
            Title = show.title,
            Path = show.path,
            DateAdded = !string.IsNullOrEmpty(show.added) ? DateTime.Parse(show.added) : null
        };

        _dbContext.Shows.Add(showEntity);
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
            var seasonEntity = await GetOrCreateSeason(showEntity, season);
            await ProcessEpisodes(show, season, seasonEntity);
        }
    }

    private async Task<Season> GetOrCreateSeason(Show showEntity, SonarrSeason season)
    {
        var seasonEntity = await _dbContext.Seasons
            .Include(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.ShowId == showEntity.Id && s.SeasonNumber == season.seasonNumber);
        if (seasonEntity != null)
        {
            return seasonEntity;
        }

        seasonEntity = new Season
        {
            SeasonNumber = season.seasonNumber,
            Path = $"{showEntity.Path}/Season {season.seasonNumber}",
            Show = showEntity
        };

        showEntity.Seasons.Add(seasonEntity);

        return seasonEntity;
    }

    private async Task ProcessEpisodes(SonarrShow show, SonarrSeason season, Season seasonEntity)
    {
        var episodes = await _sonarrService.GetEpisodes(show.id, season.seasonNumber);
        if(episodes == null) return;
        
        foreach (var episode in episodes.Where(e => e.hasFile && seasonEntity.Episodes.All(se => se.SonarrId != e.id)))
        {
            var episodePath = await _sonarrService.GetEpisodePath(episode.id);
            var episodeEntity = new Episode
            {
                SonarrId = episode.id,
                EpisodeNumber = episode.episodeNumber,
                Title = episode.title,
                FileName = Path.GetFileNameWithoutExtension(episodePath?.episodeFile?.path ?? string.Empty),
                Path = Path.GetDirectoryName(episodePath?.episodeFile?.path) ?? string.Empty,
                Season = seasonEntity
            };

            seasonEntity.Episodes.Add(episodeEntity);
        }
    }
}