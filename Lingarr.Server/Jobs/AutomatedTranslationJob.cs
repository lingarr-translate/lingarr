using Hangfire;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Core.Interfaces;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class AutomatedTranslationJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<AutomatedTranslationJob> _logger;
    private readonly IMediaSubtitleProcessor _mediaSubtitleProcessor;
    private readonly ISettingService _settingService;
    private readonly IScheduleService _scheduleService;
    private int _maxTranslationsPerRun = 10;
    private TimeSpan _defaultMovieAgeThreshold;
    private TimeSpan _defaultShowAgeThreshold;

    public AutomatedTranslationJob(
        LingarrDbContext dbContext,
        ILogger<AutomatedTranslationJob> logger,
        IMediaSubtitleProcessor mediaSubtitleProcessor,
        IScheduleService scheduleService,
        ISettingService settingService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _settingService = settingService;
        _scheduleService = scheduleService;
        _mediaSubtitleProcessor = mediaSubtitleProcessor;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 0)]
    [Queue("translation")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

        var settings = await _settingService.GetSettings([
            SettingKeys.Automation.AutomationEnabled,
            SettingKeys.Automation.TranslationCycle,
            SettingKeys.Automation.MaxTranslationsPerRun,
            SettingKeys.Automation.MovieAgeThreshold,
            SettingKeys.Automation.ShowAgeThreshold
        ]);

        if (settings[SettingKeys.Automation.AutomationEnabled] == "false")
        {
            _logger.LogInformation("Automation not enabled, skipping translation automation.");
            return;
        }

        int.TryParse(settings[SettingKeys.Automation.MaxTranslationsPerRun], out int maxTranslations);
        int.TryParse(settings[SettingKeys.Automation.MovieAgeThreshold], out int movieAgeThreshold);
        int.TryParse(settings[SettingKeys.Automation.ShowAgeThreshold], out int showAgeThreshold);

        _maxTranslationsPerRun = maxTranslations;
        _defaultMovieAgeThreshold = TimeSpan.FromHours(movieAgeThreshold);
        _defaultShowAgeThreshold = TimeSpan.FromHours(showAgeThreshold);

        var translationCycle = settings[SettingKeys.Automation.TranslationCycle] == "true" ? "movies" : "shows";
        _logger.LogInformation($"Starting translation cycle for |Green|{translationCycle}|/Green|");

        switch (translationCycle)
        {
            case "movies":
                await _settingService.SetSetting(SettingKeys.Automation.TranslationCycle, "false");
                if (!await ProcessMovies())
                {
                    await ProcessShows();
                }

                break;
            case "shows":
                await _settingService.SetSetting(SettingKeys.Automation.TranslationCycle, "true");
                if (!await ProcessShows())
                {
                    await ProcessMovies();
                }

                break;
        }

        await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
    }

    private bool ShouldProcessMedia(IMedia media, MediaType mediaType, TimeSpan? customAgeThreshold = null)
    {
        if (media.Path == null)
        {
            return false;
        }

        var fileInfo = new FileInfo(media.Path);
        var fileAge = DateTime.UtcNow - fileInfo.LastWriteTime.ToUniversalTime();

        var threshold = customAgeThreshold ??
                        (mediaType == MediaType.Movie ? _defaultMovieAgeThreshold : _defaultShowAgeThreshold);

        var fileAgeHours = fileAge.TotalHours;
        var thresholdHours = threshold.TotalHours;
        if (!(fileAgeHours < thresholdHours))
        {
            return true;
        }

        _logger.LogInformation(
            "Media {FileName} does not meet age threshold. Age: {Age} hours, Required: {Threshold} hours",
            media.FileName,
            fileAgeHours.ToString("F2"),
            thresholdHours.ToString("F2"));
        return false;
    }

    private async Task<bool> ProcessMovies()
    {
        _logger.LogInformation("Movie Translation job initiated");

        var movies = await _dbContext.Movies
            .Where(movie => !movie.ExcludeFromTranslation)
            .OrderBy(movie => movie.UpdatedAt)
            .ToListAsync();

        if (!movies.Any())
        {
            _logger.LogInformation("No translatable movies found, starting show translation.");
            return false;
        }

        // We take a subset so that the latest updated at aren't included yet.
        var candidates = movies.Take(_maxTranslationsPerRun * 4).ToList();
        var random = new Random();
        var randomSelection = candidates.OrderBy(x => random.Next()).Take(_maxTranslationsPerRun).ToList();

        var translationsInitiated = 0;
        foreach (var movie in randomSelection)
        {
            try
            {
                if (translationsInitiated >= _maxTranslationsPerRun)
                {
                    _logger.LogInformation("Max translations per run reached. Stopping translation process.");
                    break;
                }

                TimeSpan? threshold = movie.TranslationAgeThreshold.HasValue
                    ? TimeSpan.FromHours(movie.TranslationAgeThreshold.Value)
                    : null;

                if (!ShouldProcessMedia(movie, MediaType.Movie, threshold))
                {
                    continue;
                }

                var isProcessed = await _mediaSubtitleProcessor.ProcessMedia(movie, MediaType.Movie);
                if (isProcessed)
                {
                    translationsInitiated++;
                }
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogWarning("Directory not found at path: |Red|{Path}|/Red|, skipping subtitle", movie.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing subtitles for movie at path: |Red|{Path}|/Red|, skipping subtitle",
                    movie.Path);
            }
        }

        return true;
    }

    private async Task<bool> ProcessShows()
    {
        _logger.LogInformation("Show Translation job initiated");

        var shows = await _dbContext.Shows
            .Where(show => !show.ExcludeFromTranslation)
            .ToListAsync();

        var seasons = await _dbContext.Seasons
            .Where(season => shows.Select(s => s.Id).Contains(season.ShowId) && !season.ExcludeFromTranslation)
            .ToListAsync();

        var episodes = await _dbContext.Episodes
            .Where(episode =>
                seasons.Select(s => s.Id).Contains(episode.SeasonId) && !episode.ExcludeFromTranslation)
            .OrderBy(e => e.UpdatedAt)
            .ToListAsync();

        if (!episodes.Any())
        {
            _logger.LogInformation("No translatable shows found, starting movie translation.");
            return false;
        }

        // We take a subset so that the latest updated at aren't included yet.
        var candidates = episodes.Take(_maxTranslationsPerRun * 4).ToList();
        var random = new Random();
        var randomSelection = candidates.OrderBy(x => random.Next()).Take(_maxTranslationsPerRun).ToList();

        var translationsInitiated = 0;
        foreach (var episode in randomSelection)
        {
            if (translationsInitiated >= _maxTranslationsPerRun)
            {
                _logger.LogInformation("Max translations per run reached. Stopping translation process.");
                break;
            }

            try
            {
                var season = seasons.FirstOrDefault(s => s.Id == episode.SeasonId);
                var show = shows.FirstOrDefault(s => s.Id == season?.ShowId);

                TimeSpan? threshold = null;
                if (show != null && show.TranslationAgeThreshold.HasValue)
                {
                    threshold = TimeSpan.FromHours(show.TranslationAgeThreshold.Value);
                }

                if (!ShouldProcessMedia(episode, MediaType.Episode, threshold))
                {
                    continue;
                }

                var isProcessed = await _mediaSubtitleProcessor.ProcessMedia(episode, MediaType.Episode);
                if (isProcessed)
                {
                    translationsInitiated++;
                }
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogWarning("Directory not found for show at path: |Red|{Path}|/Red|, skipping episode",
                    episode.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing subtitles for episode at path: |Red|{Path}|/Red|, skipping episode",
                    episode.Path);
            }
        }

        return true;
    }
}