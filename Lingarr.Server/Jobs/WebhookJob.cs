using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class WebhookJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly IMediaService _mediaService;
    private readonly IMediaSubtitleProcessor _mediaSubtitleProcessor;
    private readonly ILogger<WebhookJob> _logger;

    public WebhookJob(
        LingarrDbContext dbContext,
        IMediaService mediaService,
        IMediaSubtitleProcessor mediaSubtitleProcessor,
        ILogger<WebhookJob> logger)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
        _mediaSubtitleProcessor = mediaSubtitleProcessor;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 2 * 60)]
    [AutomaticRetry(Attempts = 3)]
    [Queue("webhook")]
    public async Task ProcessRadarrWebhook(RadarrWebhookPayload payload)
    {
        if (payload.Movie == null)
        {
            _logger.LogWarning("Radarr webhook payload has no movie data. Skipping.");
            return;
        }

        try
        {
            var movieId = await _mediaService.GetMovieIdOrSyncFromRadarrMovieId(payload.Movie.Id);
            if (movieId == 0)
            {
                _logger.LogWarning("Failed to sync or find movie with Radarr ID {RadarrId}. Movie may not have a file yet.",
                    payload.Movie.Id);
                return;
            }
            
            var movie = await _dbContext.Movies.FirstOrDefaultAsync(m => m.Id == movieId);
            if (movie == null)
            {
                _logger.LogError("Movie with ID {MovieId} not found in database after sync", movieId);
                return;
            }

            await _mediaSubtitleProcessor.ProcessMedia(movie, MediaType.Movie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Radarr webhook for movie {MovieTitle} (Radarr ID: {RadarrId})",
                payload.Movie.Title, payload.Movie.Id);
            throw;
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 2 * 60)]
    [AutomaticRetry(Attempts = 3)]
    [Queue("webhook")]
    public async Task ProcessSonarrWebhook(SonarrWebhookPayload payload)
    {
        if (payload.Series == null || payload.Episodes == null || !payload.Episodes.Any())
        {
            _logger.LogWarning("Sonarr webhook payload has no series or episode data. Skipping.");
            return;
        }

        try
        {
            foreach (var episode in payload.Episodes)
            {
                var episodeId = await _mediaService.GetEpisodeIdOrSyncFromSonarrEpisodeId(episode.Id);
                if (episodeId == 0)
                {
                    _logger.LogWarning("Failed to sync or find episode with Sonarr ID {SonarrEpisodeId}. Episode may not have a file yet.",
                        episode.Id);
                    continue;
                }
                
                var episodeEntity = await _dbContext.Episodes.FirstOrDefaultAsync(e => e.Id == episodeId);
                if (episodeEntity == null)
                {
                    _logger.LogError("Episode with ID {EpisodeId} not found in database after sync", episodeId);
                    continue;
                }

                await _mediaSubtitleProcessor.ProcessMedia(episodeEntity, MediaType.Episode);
            }

            _logger.LogInformation("Completed processing Sonarr webhook for series: {SeriesTitle}", payload.Series.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Sonarr webhook for series {SeriesTitle} (Sonarr ID: {SonarrId})",
                payload.Series.Title, payload.Series.Id);
            throw;
        }
    }
}
