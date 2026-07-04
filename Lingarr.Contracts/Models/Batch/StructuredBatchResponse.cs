using System.Text.Json.Serialization;

namespace Lingarr.Contracts.Models.Batch;

public class StructuredBatchResponse
{
    [JsonPropertyName("line")]
    public string Line { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public int Position { get; set; }
}
