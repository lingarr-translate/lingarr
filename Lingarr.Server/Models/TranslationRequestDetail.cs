using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class TranslationRequestDetail
{
    public int Id { get; set; }
    public string? JobId { get; set; }
    public int? MediaId { get; set; }
    public required string Title { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public string? SubtitleToTranslate { get; set; }
    public string? TranslatedSubtitle { get; set; }
    public required MediaType MediaType { get; set; }
    public required TranslationStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TranslationRequestEventDetail> Events { get; set; } = [];
    public List<TranslationRequestSubtitleLines> Lines { get; set; } = [];
}