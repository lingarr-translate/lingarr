namespace Lingarr.Server.Models.Extraction;

public class SubtitleStream
{
    public int Index { get; init; }
    public required string Codec { get; init; }
    public string? Language { get; init; }
    public string? Title { get; init; }
    public bool Forced { get; init; }
    public bool HearingImpaired { get; init; }
}
