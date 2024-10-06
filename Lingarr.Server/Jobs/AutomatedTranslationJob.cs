using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;

namespace Lingarr.Server.Jobs;

public class AutomatedTranslationJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<AutomatedTranslationJob> _logger;
    private readonly MediaSubtitleProcessor _mediaSubtitleProcessor;
    private readonly ISettingService _settingService;
    private int _maxTranslationsPerRun = 10;

    public AutomatedTranslationJob(LingarrDbContext dbContext,
        ILogger<AutomatedTranslationJob> logger,
        MediaSubtitleProcessor mediaSubtitleProcessor,
        ISettingService settingService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _settingService = settingService;
        _mediaSubtitleProcessor = mediaSubtitleProcessor;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task Execute()
    {
        var settings =
            await _settingService.GetSettings(["automation_enabled", "translation_cycle", "max_translations_per_run"]);
        int.TryParse(settings["max_translations_per_run"], out int maxTranslations);
        _maxTranslationsPerRun = maxTranslations;

        if (settings["automation_enabled"] == "false")
        {
            _logger.LogInformation("Automation not enabled, skipping translation automation.");
            return;
        }

        var translationCycle = settings["translation_cycle"] == "true" ? "movies" : "shows";
        _logger.LogInformation($"Starting translation cycle for |Green|{translationCycle}|/Green|");
        switch (translationCycle)
        {
            case "movies":
                await _settingService.SetSetting("translation_cycle", "false");
                await ProcessMovies();
                break;
            case "shows":

                await _settingService.SetSetting("translation_cycle", "true");
                await ProcessShows();
                break;
        }
    }

    private async Task ProcessMovies()
    {
        _logger.LogInformation("Movie Translation job initiated");
        var movies = _dbContext.Movies.ToList();
        if (!movies.Any())
        {
            _logger.LogInformation("No translatable movies found, starting show translation.");
            await ProcessShows();
            return;
        }

        int translationsInitiated = 0;
        foreach (var movie in movies)
        {
            if (translationsInitiated >= _maxTranslationsPerRun)
            {
                _logger.LogInformation("Max translations per run reached. Stopping translation process.");
                break;
            }

            var isProcessed = await _mediaSubtitleProcessor.ProcessMediaAsync(movie);
            if (isProcessed)
            {
                translationsInitiated++;
            }
        }
    }

    private async Task ProcessShows()
    {
        _logger.LogInformation("Show Translation job initiated");
        var episodes = _dbContext.Shows
            .SelectMany(s => s.Seasons)
            .SelectMany(season => season.Episodes)
            .ToList();
        if (!episodes.Any())
        {
            _logger.LogInformation("No translatable shows found, starting movie translation.");
            await ProcessMovies();
            return;
        }

        int translationsInitiated = 0;
        foreach (var episode in episodes)
        {
            if (translationsInitiated >= _maxTranslationsPerRun)
            {
                _logger.LogInformation("Max translations per run reached. Stopping translation process.");
                break;
            }

            var isProcessed = await _mediaSubtitleProcessor.ProcessMediaAsync(episode);
            if (isProcessed)
            {
                translationsInitiated++;
            }
        }
    }
}