using Lingarr.Core.Enum;
using Lingarr.Server.Models.Batch.Request;

namespace Lingarr.Server.Models.FileSystem;

public class TranslateAbleSubtitleContent
{
    public required int ArrMediaId { get; set; }
    public required string Title { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public required MediaType MediaType { get; set; }
    public required List<BatchSubtitleLine> Lines { get; set; }
}