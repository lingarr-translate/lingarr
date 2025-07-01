using Lingarr.Server.Models.Batch;

namespace Lingarr.Server.Interfaces.Services.Translation;

public interface IBatchTranslationService
{
    /// <summary>
    /// Translates a batch of subtitle items in a single request
    /// </summary>
    Task<Dictionary<int, string>> TranslateBatchAsync(
        List<BatchSubtitleItem> subtitleBatch,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken);
}