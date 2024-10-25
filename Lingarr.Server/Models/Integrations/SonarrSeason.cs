using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class SonarrSeason
{
    [JsonPropertyName("seasonNumber")]
    public required int SeasonNumber { get; set; }
}