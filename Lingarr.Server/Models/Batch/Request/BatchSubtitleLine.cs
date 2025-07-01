using System.ComponentModel.DataAnnotations;

namespace Lingarr.Server.Models.Batch.Request;

/// <summary>
/// Represents a subtitle item for translation
/// </summary>
public class BatchSubtitleLine
{
    /// <summary>
    /// Position or index identifier of the subtitle
    /// </summary>
    [Required]
    public int Position { get; set; }

    /// <summary>
    /// Line to translate
    /// </summary>
    [Required]
    public string Line { get; set; }
}