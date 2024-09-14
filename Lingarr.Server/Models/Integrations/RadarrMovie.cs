namespace Lingarr.Server.Models.Integrations;

public class RadarrMovie
{
    public required int id { get; set; } // id
    public required string title { get; set; } // title
    public required string path { get; set; } // path
    public required string added { get; set; } // added
    public required bool hasFile { get; set; } // hasFile
    public RadarrMovieFile movieFile { get; set; } = new(); // hasFile
    public List<MediaImage> images { get; set; } = new(); // images
}