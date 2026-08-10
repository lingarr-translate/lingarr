namespace Lingarr.Server.Models;

public class ProofreadLineRequest
{
    public required string SourceLine { get; set; }
    public required string TranslatedLine { get; set; }
    public required string SourceLanguage { get; set; }
    public required string TargetLanguage { get; set; }
}
