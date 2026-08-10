namespace Lingarr.Contracts.Translation;

/// <summary>
/// Optional capability that can be implemented by translation providers able to review an
/// existing translation against its source line and return a corrected translation.
/// </summary>
public interface IProofreadService
{
    /// <summary>
    /// Reviews a translated line against its source line and returns the corrected translation,
    /// or the translation unchanged when nothing needs correcting.
    /// </summary>
    Task<string> ProofreadAsync(
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken);
}
