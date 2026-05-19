using Lingarr.Core.Enum;

namespace Lingarr.Core.Entities;

public class TranslationRequestLine : BaseEntity
{
    public int TranslationRequestId { get; set; }
    public TranslationRequest TranslationRequest { get; set; } = null!;
    public int Position { get; set; }
    public required string Source { get; set; }
    public required string Target { get; set; }
    public string? Service { get; set; }
    public string? MatchedSource { get; set; }
    public string? MatchedTarget { get; set; }
    public MatchTier? Tier { get; set; }
}
