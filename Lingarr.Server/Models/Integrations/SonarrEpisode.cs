using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class SonarrEpisode
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }
    [JsonPropertyName("episodeNumber")]
    public required int EpisodeNumber { get; set; }
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    [JsonPropertyName("seasonNumber")]
    public required int SeasonNumber { get; set; }
    [JsonPropertyName("hasFile")]
    public required bool HasFile { get; set; }
    [JsonPropertyName("series")]
    public SonarrShow? Show { get; set; }
}