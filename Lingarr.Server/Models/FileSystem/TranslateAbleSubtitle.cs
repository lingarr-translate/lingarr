namespace Lingarr.Server.Models.FileSystem;

public class TranslateAbleSubtitle
{
    public required string SubtitlePath { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
}