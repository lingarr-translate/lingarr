using Lingarr.Contracts.Models;

namespace Lingarr.Server.Models;

public class TranslatedLineData
{
    public int Position { get; set; }
    public required string Source { get; set; }
    public required string Target { get; set; }
    public string? Service { get; set; }
    public LanguagePair? Pair { get; set; }
}
