using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class RadarrMovieFile
{
    [JsonPropertyName("path")]
    public string? Path { get; set; } = string.Empty;
}