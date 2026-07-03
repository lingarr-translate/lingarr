using Lingarr.Contracts.Models;

namespace Lingarr.Server.Models;

public class LanguageMatch
{
    public required string Code { get; set; }
    public required MatchTier Tier { get; set; }
}
