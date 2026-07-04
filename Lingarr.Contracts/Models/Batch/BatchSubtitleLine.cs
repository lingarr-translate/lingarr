using System.ComponentModel.DataAnnotations;

namespace Lingarr.Contracts.Models.Batch;

/// <summary>
/// A subtitle line accepted by the batch translation HTTP request body.
/// </summary>
public class BatchSubtitleLine
{
    [Required]
    public int Position { get; set; }

    [Required]
    public string Line { get; set; } = string.Empty;
}
