namespace Lingarr.Server.Models.Batch.Response;

/// <summary>
/// Represents a translated subtitle item
/// </summary>
public class BatchTranslatedLine
{
    /// <summary>
    /// Position or index identifier matching the original subtitle
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Translated line
    /// </summary>
    public string Line { get; set; }
}