namespace Lingarr.Server.Models.Integrations;

public class SonarrEpisodePath
{
    public SonarrEpisodeFile episodeFile { get; set; } = new(); // episode | episodefile -> path
}