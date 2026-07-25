using Lingarr.Contracts.Models.Batch;
using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.FileSystem;

public class TranslateAbleSubtitleContent
{
    public required int ArrMediaId { get; set; }
    /// <summary>
    /// Optional override. When null/empty, TranslateContentAsync derives the
    /// canonical title via FormatMediaTitle after resolving MediaId.
    /// </summary>
    public string? Title { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
    public required MediaType MediaType { get; set; }
    public required List<BatchSubtitleLine> Lines { get; set; }

    /// <summary>
    /// Optional source subtitle path for integrators (e.g. Bazarr) that translate line
    /// batches but still know the on-disk SRT. Shown on the Translations UI when set.
    /// </summary>
    public string? SourceSubtitlePath { get; set; }

    /// <summary>
    /// Optional path where the client will write (or has written) the translated file.
    /// </summary>
    public string? TranslatedSubtitlePath { get; set; }
}
