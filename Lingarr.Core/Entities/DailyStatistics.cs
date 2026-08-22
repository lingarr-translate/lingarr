
namespace Lingarr.Core.Entities;

public class DailyStatistics : BaseEntity
{
    public required DateOnly Date { get; set; }
    public int TranslationCount { get; set; }
}