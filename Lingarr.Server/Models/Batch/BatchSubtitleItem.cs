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
    public string Line { get; set; } = string.Empty;
    
    /// <summary>
    /// If true, this line is context-only and should NOT be translated.
    /// The AI should use it for understanding conversational flow but not include it in output.
    /// </summary>
    public bool IsContextOnly { get; set; } = false;
}