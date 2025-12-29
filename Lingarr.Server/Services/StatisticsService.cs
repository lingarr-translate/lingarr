using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Batch.Response;
using Lingarr.Server.Models.FileSystem;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class StatisticsService : IStatisticsService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISettingService _settingService;

    public StatisticsService(
        LingarrDbContext dbContext,
        ISettingService settingService)
    {
        _dbContext = dbContext;
        _settingService = settingService;
    }

    public async Task<Statistics> GetStatistics()
    {
        return await GetOrCreateStatistics();
    }

    public async Task<IEnumerable<DailyStatistics>> GetDailyStatistics(int days = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1); // +1 to include today
        var stats = await _dbContext.DailyStatistics
            .Where(d => d.Date >= startDate)
            .OrderBy(d => d.Date)
            .ToListAsync();

        return stats;
    }

    private async Task<Statistics> GetOrCreateStatistics()
    {
        var stats = await _dbContext.Statistics.SingleOrDefaultAsync();
        if (stats == null)
        {
            stats = new Statistics();
            _dbContext.Statistics.Add(stats);
            await _dbContext.SaveChangesAsync();
        }

        return stats;
    }

    private async Task<DailyStatistics> GetOrCreateDailyStatistics(DateTime today)
    {
        var dailyStats = await _dbContext.DailyStatistics
            .Where(d => d.Date >= today)
            .FirstOrDefaultAsync();

        if (dailyStats == null)
        {
            dailyStats = new DailyStatistics { Date = today };
            _dbContext.DailyStatistics.Add(dailyStats);
        }

        return dailyStats;
    }

    public async Task<int> UpdateTranslationStatisticsFromSubtitles(
        TranslationRequest request,
        string serviceType,
        string? modelName,
        List<SubtitleItem> translatedSubtitles)
    {
        var lineCount = translatedSubtitles.Sum(s => s.Lines.Count);
        var charCount = translatedSubtitles.Sum(s => s.Lines.Sum(l => l.Length));

        return await UpdateTranslationStatisticsInternal(
            request, serviceType, modelName, lineCount, charCount);
    }

    public async Task<int> UpdateTranslationStatisticsFromLines(
        TranslationRequest request,
        string serviceType,
        string? modelName,
        BatchTranslatedLine[] translatedLines)
    {
        var lineCount = translatedLines.Length;
        var charCount = translatedLines.Sum(s => s.Line.Length);

        return await UpdateTranslationStatisticsInternal(
            request, serviceType, modelName, lineCount, charCount);
    }

    private async Task<int> UpdateTranslationStatisticsInternal(
        TranslationRequest request,
        string serviceType,
        string? modelName,
        int totalLines,
        int totalCharacters)
    {
        var stats = await GetOrCreateStatistics();
        var today = DateTime.UtcNow.Date;

        // Update total counts
        stats.TotalLinesTranslated += totalLines;
        stats.TotalCharactersTranslated += totalCharacters;
        stats.TotalFilesTranslated++;

        // Update media type statistics
        var mediaType = request.MediaType.ToString();
        var mediaTypeStats = stats.TranslationsByMediaType;
        mediaTypeStats[mediaType] = mediaTypeStats.GetValueOrDefault(mediaType) + 1;
        stats.TranslationsByMediaType = mediaTypeStats;

        // Update service type statistics
        var serviceStats = stats.TranslationsByService;
        serviceStats[serviceType] = serviceStats.GetValueOrDefault(serviceType) + 1;
        stats.TranslationsByService = serviceStats;

        // Update language pair statistics 
        var languageStats = stats.SubtitlesByLanguage;
        var languagePair = $"{request.SourceLanguage}:{request.TargetLanguage}";
        languageStats[languagePair] = languageStats.GetValueOrDefault(languagePair) + 1;
        stats.SubtitlesByLanguage = languageStats;

        // Update model statistics (correlated with service)
        if (!string.IsNullOrEmpty(modelName))
        {
            var modelStats = stats.TranslationsByModel;
            var serviceModelKey = $"{serviceType}:{modelName}";
            modelStats[serviceModelKey] = modelStats.GetValueOrDefault(serviceModelKey) + 1;
            stats.TranslationsByModel = modelStats;
        }

        // Update daily statistics
        var dailyStats = await GetOrCreateDailyStatistics(today);
        dailyStats.TranslationCount++;

        return await _dbContext.SaveChangesAsync();
    }

    public async Task ResetStatistics()
    {
        // Reset the main statistics entity
        var stats = await GetOrCreateStatistics();
        stats.TotalLinesTranslated = 0;
        stats.TotalCharactersTranslated = 0;
        stats.TotalFilesTranslated = 0;
        stats.TotalMovies = 0;
        stats.TotalEpisodes = 0;
        stats.TotalSubtitles = 0;
        stats.TranslationsByMediaType = new Dictionary<string, int>();
        stats.TranslationsByService = new Dictionary<string, int>();
        stats.SubtitlesByLanguage = new Dictionary<string, int>();
        stats.TranslationsByModel = new Dictionary<string, int>();

        // Delete all daily statistics
        var dailyStats = await _dbContext.DailyStatistics.ToListAsync();
        _dbContext.DailyStatistics.RemoveRange(dailyStats);

        // Reset telemetry snapshot settings
        var telemetrySettings = new Dictionary<string, string>
        {
            { "telemetry_last_reported_lines", "0" },
            { "telemetry_last_reported_files", "0" },
            { "telemetry_last_reported_characters", "0" }
        };
        await _settingService.SetSettings(telemetrySettings);

        await _dbContext.SaveChangesAsync();
    }
}