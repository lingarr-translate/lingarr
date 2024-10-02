namespace Lingarr.Server.Models.Integrations;

public class SonarrEpisode
{
    public required int id { get; set; } // episode | id
    public required int episodeNumber { get; set; } // episode | episodeNumber
    public required string title { get; set; } // episode |title
    public required int seasonNumber { get; set; } // episode |seasonNumber
    public required bool hasFile { get; set; } // episode | hasFile
}