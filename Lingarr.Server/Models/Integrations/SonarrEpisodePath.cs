using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class SonarrEpisodePath
{
    [JsonPropertyName("episodeFile")]
    public SonarrEpisodeFile EpisodeFile { get; set; } = new();
}