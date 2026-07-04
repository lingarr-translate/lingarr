using System.ComponentModel.DataAnnotations;

namespace Lingarr.Contracts.Models.Batch;

/// <summary>
/// HTTP request body for the batch translation endpoint.
/// </summary>
public class BatchTranslationRequest
{
    [Required]
    public string SourceLanguage { get; set; } = string.Empty;

    [Required]
    public string TargetLanguage { get; set; } = string.Empty;

    [Required]
    public List<BatchSubtitleLine> Lines { get; set; } = new();
}
