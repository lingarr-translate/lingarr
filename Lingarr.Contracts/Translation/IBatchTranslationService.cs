using Lingarr.Contracts.Models.Batch;

namespace Lingarr.Contracts.Translation;

/// <summary>
/// Optional capability that can be implemented by translation providers 
/// supporting batch translation of multiple subtitle lines in a single request.
/// </summary>
public interface IBatchTranslationService
{
    /// <summary>
    /// Translates multiple subtitle lines in a single request and returns 
    /// the translated text indexed by their original position.
    /// </summary>
    Task<Dictionary<int, string>> TranslateBatchAsync(
        List<BatchSubtitleItem> subtitleBatch,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken);
}
