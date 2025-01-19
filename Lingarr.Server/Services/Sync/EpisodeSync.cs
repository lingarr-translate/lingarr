using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Sync;

public class EpisodeSync : IEpisodeSync
{
    private readonly ISonarrService _sonarrService;
    private readonly PathConversionService _pathConversionService;

    public EpisodeSync(
        ISonarrService sonarrService,
        PathConversionService pathConversionService)
    {
        _sonarrService = sonarrService;
        _pathConversionService = pathConversionService;
    }

    /// <inheritdoc />
    public async Task SyncEpisodes(SonarrShow show, Season season)
    {
        var episodes = await _sonarrService.GetEpisodes(show.Id, season.SeasonNumber);
        if (episodes == null) return;

        foreach (var episode in episodes.Where(e => e.HasFile))
        {
            var episodePathResult = await _sonarrService.GetEpisodePath(episode.Id);
            var episodePath = _pathConversionService.ConvertAndMapPath(
                episodePathResult?.EpisodeFile.Path ?? string.Empty,
                MediaType.Show
            );
            
            SyncEpisode(episode, episodePath, season);
        }

        RemoveNonExistentEpisodes(season, episodes);
    }

    /// <summary>
    /// Synchronizes a single episode with the database, creating or updating the episode entity as needed
    /// </summary>
    /// <param name="episode">The Sonarr episode containing the source data</param>
    /// <param name="episodePath">The converted and mapped file path for the episode</param>
    /// <param name="season">The season entity that owns this episode</param>
    private static void SyncEpisode(SonarrEpisode episode, string episodePath, Season season)
    {
        var episodeEntity = season.Episodes.FirstOrDefault(se => se.SonarrId == episode.Id);
        if (episodeEntity == null)
        {
            episodeEntity = new Episode
            {
                SonarrId = episode.Id,
                EpisodeNumber = episode.EpisodeNumber,
                Title = episode.Title,
                FileName = Path.GetFileNameWithoutExtension(episodePath),
                Path = Path.GetDirectoryName(episodePath),
                Season = season
            };
            season.Episodes.Add(episodeEntity);
        }
        else
        {
            episodeEntity.EpisodeNumber = episode.EpisodeNumber;
            episodeEntity.Title = episode.Title;
            episodeEntity.FileName = Path.GetFileNameWithoutExtension(episodePath);
            episodeEntity.Path = Path.GetDirectoryName(episodePath);
        }
    }

    /// <summary>
    /// Removes episodes from the season that no longer exist in Sonarr
    /// </summary>
    /// <param name="season">The season entity containing the episodes to check</param>
    /// <param name="currentEpisodes">The list of currently existing Sonarr episodes</param>
    private static void RemoveNonExistentEpisodes(Season season, List<SonarrEpisode> currentEpisodes)
    {
        var episodesToRemove = season.Episodes
            .Where(seasonEpisode => currentEpisodes.All(episode => episode.Id != seasonEpisode.SonarrId))
            .ToList();

        foreach (var episodeToRemove in episodesToRemove)
        {
            season.Episodes.Remove(episodeToRemove);
        }
    }
}