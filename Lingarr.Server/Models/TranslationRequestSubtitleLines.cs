namespace Lingarr.Server.Models;

public class TranslationRequestSubtitleLines
{
    public int Position { get; set; }
    public required string Source { get; set; }
    public required string Target { get; set; }
}