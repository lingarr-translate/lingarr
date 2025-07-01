using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Batch.Response;

/// <summary>
/// Translated subtitle result model
/// </summary>
class StructuredBatchResponse
{
    [JsonPropertyName("line")]
    public string Line { get; set; } = string.Empty;
    
    [JsonPropertyName("position")]
    public int Position { get; set; }
}