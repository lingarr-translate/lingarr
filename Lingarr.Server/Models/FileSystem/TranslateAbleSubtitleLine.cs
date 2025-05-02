namespace Lingarr.Server.Models.FileSystem;

public class TranslateAbleSubtitleLine
{
    public required string SubtitleLine { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public List<string>? ContextLinesBefore { get; set; }
    public List<string>? ContextLinesAfter { get; set; }
}