using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Extraction;

public class ProbeStream
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("codec_name")]
    public string? CodecName { get; set; }

    [JsonPropertyName("disposition")]
    public Dictionary<string, int>? Disposition { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}
