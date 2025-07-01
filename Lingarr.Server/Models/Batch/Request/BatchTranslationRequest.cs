using System.ComponentModel.DataAnnotations;

namespace Lingarr.Server.Models.Batch.Request;

/// <summary>
/// Subtitle translation request model
/// </summary>
public class BatchTranslationRequest
{
    /// <summary>
    /// Source language code
    /// </summary>
    [Required]
    public string SourceLanguage { get; set; }

    /// <summary>
    /// Target language code
    /// </summary>
    [Required]
    public string TargetLanguage { get; set; }

    /// <summary>
    /// Subtitle Lines to translate
    /// </summary>
    [Required]
    public List<BatchSubtitleLine> Lines { get; set; }
}