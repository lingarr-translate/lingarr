using Hangfire;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Core.Interfaces;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class AutomatedTranslationJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<AutomatedTranslationJob> _logger;
    private readonly IMediaSubtitleProcessor _mediaSubtitleProcessor;
    private readonly ISettingService _settingService;
    private readonly IScheduleService _scheduleService;
    private readonly IMemoryCache _memoryCache;
    private int _maxTranslationsPerRun = 10;
    private TimeSpan _defaultMovieAgeThreshold;
    private TimeSpan _defaultShowAgeThreshold;

    private const string MovieProcessingIndexKey = "Automation:MovieProcessingIndex";
    private const string ShowProcessingIndexKey = "Automation:ShowProcessingIndex";

    public AutomatedTranslationJob(
        LingarrDbContext dbContext,
        ILogger<AutomatedTranslationJob> logger,
        IMediaSubtitleProcessor mediaSubtitleProcessor,
        IScheduleService scheduleService,
        ISettingService settingService,
        IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _settingService = settingService;
        _scheduleService = scheduleService;
        _mediaSubtitleProcessor = mediaSubtitleProcessor;
        _memoryCache = memoryCache;
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
            .OrderBy(movie => movie.Id)
            .ToListAsync();

        if (!movies.Any())
        {
            _logger.LogInformation("No translatable movies found, starting show translation.");
            return false;
        }
        
        // Instead of a random selection based on updatedAt, we will use a cycle so that all shows are processed.
        // Hopefully, this will prevent some shows from not being processed at all.
        var currentIndex = GetProcessingIndex(MovieProcessingIndexKey);
        if (currentIndex >= movies.Count)
        {
            currentIndex = 0;
            _logger.LogInformation("Movie processing cycle completed. Starting new cycle from the beginning.");
        }
        // Scan movies starting from `currentIndex` until we reach the maximum
        // number of translations or we processed all movies once; this ensures
        // we evaluate past the initial window when few items are eligible.
        _logger.LogInformation(
            "Processing up to {MaxTranslations} movies starting at {StartIndex} out of {TotalCount}",
            _maxTranslationsPerRun,
            currentIndex,
            movies.Count);

        var translationsInitiated = 0;
        var scannedMovies = 0;
        var index = currentIndex;

        while (translationsInitiated < _maxTranslationsPerRun && scannedMovies < movies.Count)
        {
            var movie = movies[index % movies.Count];
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
            finally
            {
                index++;
                scannedMovies++;
            }
        }

        var newIndex = index % movies.Count;
        SetProcessingIndex(MovieProcessingIndexKey, newIndex);

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
            .OrderBy(e => e.Id)
            .ToListAsync();

        if (!episodes.Any())
        {
            _logger.LogInformation("No translatable shows found, starting movie translation.");
            return false;
        }

        // Instead of a random selection based on updatedAt, we will use a cycle so that all shows are processed.
        // Hopefully, this will prevent some shows from not being processed at all.
        var currentIndex = GetProcessingIndex(ShowProcessingIndexKey);
        if (currentIndex >= episodes.Count)
        {
            currentIndex = 0;
            _logger.LogInformation("Show processing cycle completed. Starting new cycle from the beginning.");
        }
        _logger.LogInformation(
            "Processing up to {MaxTranslations} episodes starting at {StartIndex} out of {TotalCount}",
            _maxTranslationsPerRun,
            currentIndex,
            episodes.Count);

        var translationsInitiated = 0;
        var scannedEpisodes = 0;
        var episodeIndex = currentIndex;

        while (translationsInitiated < _maxTranslationsPerRun && scannedEpisodes < episodes.Count)
        {
            var episode = episodes[episodeIndex % episodes.Count];

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
            finally
            {
                episodeIndex++;
                scannedEpisodes++;
            }
        }

        var newIndex = episodeIndex % episodes.Count;
        SetProcessingIndex(ShowProcessingIndexKey, newIndex);

        return true;
    }

    private int GetProcessingIndex(string key)
    {
        if (!_memoryCache.TryGetValue(key, out int currentIndex))
        {
            currentIndex = 0;
        }
        return currentIndex;
    }
    
    private void SetProcessingIndex(string key, int value)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.NeverRemove
        };
        
        _memoryCache.Set(key, value, cacheOptions);
    }
}