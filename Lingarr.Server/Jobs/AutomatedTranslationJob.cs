﻿using Hangfire;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
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
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

        var settings =
            await _settingService.GetSettings([
                SettingKeys.Automation.AutomationEnabled,
                SettingKeys.Automation.TranslationCycle,
                SettingKeys.Automation.MaxTranslationsPerRun
            ]);
        int.TryParse(settings[SettingKeys.Automation.MaxTranslationsPerRun], out int maxTranslations);
        _maxTranslationsPerRun = maxTranslations;

        if (settings[SettingKeys.Automation.AutomationEnabled] == "false")
        {
            _logger.LogInformation("Automation not enabled, skipping translation automation.");
            return;
        }

        var translationCycle = settings[SettingKeys.Automation.TranslationCycle] == "true" ? "movies" : "shows";
        _logger.LogInformation($"Starting translation cycle for |Green|{translationCycle}|/Green|");
        switch (translationCycle)
        {
            case "movies":
                await _settingService.SetSetting(SettingKeys.Automation.TranslationCycle, "false");
                await ProcessMovies();
                break;
            case "shows":

                await _settingService.SetSetting(SettingKeys.Automation.TranslationCycle, "true");
                await ProcessShows();
                break;
        }
        await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
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
            try 
            {
                if (translationsInitiated >= _maxTranslationsPerRun)
                {
                    _logger.LogInformation("Max translations per run reached. Stopping translation process.");
                    break;
                }

                var isProcessed = await _mediaSubtitleProcessor.ProcessMedia(movie, MediaType.Movie);
                if (isProcessed)
                {
                    translationsInitiated++;
                }
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogWarning("Directory not found at path: |Red|{Path}|/Red|, skipping subtitle",  movie.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subtitles for movie at path: |Red|{Path}|/Red|, skipping subtitle", 
                    movie.Path);
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
            try 
            {
                if (translationsInitiated >= _maxTranslationsPerRun)
                {
                    _logger.LogInformation("Max translations per run reached. Stopping translation process.");
                    break;
                }

                var isProcessed = await _mediaSubtitleProcessor.ProcessMedia(episode, MediaType.Episode);
                if (isProcessed)
                {
                    translationsInitiated++;
                }
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogWarning("Directory not found for show at path: |Red|{Path}|/Red|, skipping episode", episode.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subtitles for episode at path: |Red|{Path}|/Red|, skipping episode", 
                    episode.Path);
            }
        }
    }
}