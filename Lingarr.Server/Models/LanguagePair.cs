using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

/// <summary>
/// A pair of language codes in a translation service's native wire format,
/// derived from a request's canonical source/target codes.
/// </summary>
public class LanguagePair
{
    public required string Source { get; set; }
    public required string Target { get; set; }
    public MatchTier Tier { get; set; }
}
