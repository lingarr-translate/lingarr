namespace Lingarr.Server.Models;

public class SubtitleValidationOptions
{
    public long MaxFileSizeBytes { get; set; }
    public int MaxSubtitleLength { get; set; }
    public int MinSubtitleLength { get; set; }
    public double MinDurationMs { get; set; }
    public double MaxDurationSecs { get; set; }
    public bool StripSubtitleFormatting { get; set; }
}