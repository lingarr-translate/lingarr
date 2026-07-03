using Lingarr.Contracts.Models;

namespace Lingarr.Contracts.Translation;

/// <summary>
/// Core interface for subtitle translation providers.
/// Implementations can be built-in Lingarr services or third-party plugins.
/// </summary>
public interface ITranslationService
{
    string? ModelName { get; }

    /// <summary>
    /// Translates a single piece of text from the source language to the target language.
    /// Optionally accepts surrounding subtitle lines as context.
    /// </summary>
    Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the list of supported source languages and their available target languages.
    /// </summary>
    Task<List<SourceLanguage>> GetLanguages();

    /// <summary>
    /// Returns the list of available models for providers that expose a model catalog.
    /// </summary>
    Task<ModelsResponse> GetModels();

    /// <summary>
    /// Resolves a requested source and target language pair to the actual language codes.
    /// </summary>
    Task<LanguagePair?> GetLanguagePair(
        string requestedSource,
        string requestedTarget,
        CancellationToken cancellationToken);
}
