using Lingarr.Core.Entities;
using Lingarr.Server.Models.Batch.Response;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services;

public interface IStatisticsService
{
    Task<Statistics> GetStatistics();
    Task<IEnumerable<DailyStatistics>> GetDailyStatistics(int days = 30);
    Task<int> UpdateTranslationStatisticsFromSubtitles(
        TranslationRequest request,
        string serviceType,
        string? modelName,
        List<SubtitleItem> translatedSubtitles);
    Task<int> UpdateTranslationStatisticsFromLines(
        TranslationRequest request,
        string serviceType,
        string? modelName,
        BatchTranslatedLine[] translatedLines);
    Task ResetStatistics();
}