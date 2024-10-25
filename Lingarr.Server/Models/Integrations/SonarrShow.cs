using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class SonarrShow
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    [JsonPropertyName("path")]
    public required string Path { get; set; }
    [JsonPropertyName("added")]
    public required string Added { get; set; }
    [JsonPropertyName("seasonFolder")]
    public required bool SeasonFolder { get; set; }
    [JsonPropertyName("rootFolderPath")]
    public required string RootFolderPath { get; set; } 
    [JsonPropertyName("seasons")]
    public List<SonarrSeason> Seasons { get; set; } = new();
    [JsonPropertyName("images")]
    public List<IntegrationImage> Images { get; set; } = new();
}