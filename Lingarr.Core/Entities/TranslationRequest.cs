using Lingarr.Core.Enum;

namespace Lingarr.Core.Entities;

public class TranslationRequest : BaseEntity
{
    public string? JobId  { get; set; }
    public int? MediaId  { get; set; }
    public required string Title { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public required string SubtitleToTranslate { get; set; }
    public string? TranslatedSubtitle { get; set; }
    public required MediaType MediaType { get; set; }
    public required TranslationStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
}
