using Lingarr.Core.Entities;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services;

public interface IStatisticsService
{
    Task<Statistics> GetStatistics();
    Task<IEnumerable<DailyStatistics>> GetDailyStatistics(int days = 30);
    Task<int> UpdateTranslationStatistics(
        TranslationRequest request, 
        string serviceType, 
        List<SubtitleItem> subtitles, 
        List<SubtitleItem> translatedSubtitles);
}