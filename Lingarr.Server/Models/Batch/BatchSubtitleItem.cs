namespace Lingarr.Server.Models.Batch;

/// <summary>
/// Represents a subtitle item in a batch translation request
/// </summary>
public class BatchSubtitleItem
{
    /// <summary>
    /// Position or index identifier of the subtitle
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Line to translate
    /// </summary>
    public string Line { get; set; }
}