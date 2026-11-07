using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Extraction;

public class ProbeOutput
{
    [JsonPropertyName("streams")]
    public List<ProbeStream> Streams { get; set; } = [];
}
