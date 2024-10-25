using Lingarr.Core.Entities;
using Lingarr.Core.Enum;

namespace Lingarr.Server.Interfaces;

public interface ITranslationRequest
{
    string JobId { get; set; }
    string SourceLanguage { get; set; }
    string TargetLanguage { get; set; }
    string SubtitleToTranslate { get; set; }
    string? TranslatedSubtitle { get; set; }
    TranslationStatus Status { get; set; }
    DateTime? CompletedAt { get; set; }

    int? ShowId { get; set; }
    Show? Show { get; set; }
        
    int? MovieId { get; set; }
    Movie? Movie { get; set; }
}