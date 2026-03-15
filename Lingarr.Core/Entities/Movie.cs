using Lingarr.Core.Interfaces;

namespace Lingarr.Core.Entities;

public class Movie : BaseEntity, IMedia
{
    public required int RadarrId { get; set; }
    public required string Title { get; set; }
    public required string? FileName { get; set; }
    public required string? Path { get; set; }
    public string? MediaHash { get; set; } = string.Empty;
    public required DateTime? DateAdded { get; set; }
    public List<Image> Images { get; set; } = new();
    public bool IncludeInTranslation { get; set; } = true;
    public int? TranslationAgeThreshold { get; set; }
}