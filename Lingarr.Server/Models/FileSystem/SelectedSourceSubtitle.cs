namespace Lingarr.Server.Models.FileSystem;

public class SelectedSourceSubtitle
{
    public required Subtitles Subtitle { get; set; }
    public required string SourceLanguage { get; set; }
    public required HashSet<string> AvailableLanguages { get; set; }
}
