using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.Api;

public class TranslationRequestDto
{
    public int Id { get; set; }
    public string? JobId  { get; set; }
    public MediaType MediaType { get; set; }
    public required MediaDto Media { get; set; }
    
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public string? SubtitleToTranslate { get; set; }
    public string? TranslatedSubtitle { get; set; }

    public required TranslationStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}