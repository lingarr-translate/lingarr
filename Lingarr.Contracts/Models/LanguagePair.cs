namespace Lingarr.Contracts.Models;

/// <summary>
/// A pair of language codes expressed in a provider's native wire format,
/// derived from the request's canonical source/target codes.
/// </summary>
public class LanguagePair
{
    public required string Source { get; set; }
    public required string Target { get; set; }
    public MatchTier Tier { get; set; }
}
