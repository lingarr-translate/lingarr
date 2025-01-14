
namespace Lingarr.Core.Entities;

public class DailyStatistics : BaseEntity
{
    public required DateTime Date { get; set; }
    public int TranslationCount { get; set; }
}