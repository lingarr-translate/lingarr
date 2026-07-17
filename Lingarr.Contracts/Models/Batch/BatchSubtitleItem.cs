using System.Text.Json.Serialization;

namespace Lingarr.Contracts.Models.Batch;

/// <summary>
/// A subtitle line passed to a batch translation provider.
/// </summary>
public class BatchSubtitleItem
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("line")]
    public string Line { get; set; } = string.Empty;
}
