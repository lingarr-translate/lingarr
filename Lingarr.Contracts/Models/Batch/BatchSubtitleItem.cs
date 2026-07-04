namespace Lingarr.Contracts.Models.Batch;

/// <summary>
/// A subtitle line passed to a batch translation provider.
/// </summary>
public class BatchSubtitleItem
{
    public int Position { get; set; }
    public string Line { get; set; } = string.Empty;
}
