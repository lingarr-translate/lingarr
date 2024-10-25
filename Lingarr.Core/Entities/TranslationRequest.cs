using System.ComponentModel.DataAnnotations.Schema;
using Lingarr.Core.Enum;

namespace Lingarr.Core.Entities;

public class TranslationRequest : BaseEntity
{
    public string? JobId  { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public required string SubtitleToTranslate { get; set; }
    public string? TranslatedSubtitle { get; set; }
    public required TranslationStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int? EpisodeId { get; set; }
    [ForeignKey(nameof(EpisodeId))]
    public Episode? Episode { get; set; }
    
    public int? MovieId { get; set; }
    [ForeignKey(nameof(MovieId))]
    public Movie? Movie { get; set; }
}
