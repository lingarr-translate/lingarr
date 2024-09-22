namespace Lingarr.Server.Models.Integrations;

public class SonarrShow
{
    public required int id { get; set; } // series | id
    public required string title { get; set; } // series | title
    public required string path { get; set; } // series | path
    public required string added { get; set; } // series | added
    public required string rootFolderPath { get; set; } // series | rootFolderPath
    public List<SonarrSeason> seasons { get; set; } = new();
    public List<MediaImage> images { get; set; } = new(); // images
}