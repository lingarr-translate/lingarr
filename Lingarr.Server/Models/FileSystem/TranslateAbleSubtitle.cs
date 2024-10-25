using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.FileSystem;

public class TranslateAbleSubtitle
{
    public required int MediaId { get; set; }
    public required string SubtitlePath { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public required MediaType MediaType { get; set; }
}