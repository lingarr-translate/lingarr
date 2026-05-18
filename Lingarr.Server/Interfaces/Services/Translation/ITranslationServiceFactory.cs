namespace Lingarr.Server.Interfaces.Services.Translation;

public readonly record struct TranslationServiceEntry(string Name, ITranslationService Service, IBatchTranslationService? BatchService);

public interface ITranslationServiceFactory
{
    /// <summary>
    /// Creates and returns an instance of a translation service based on the specified service type.
    /// </summary>
    /// <param name="serviceType">The provider name (case-insensitive), e.g. "libretranslate", "openai", "anthropic", "deepl", "gemini", "deepseek", "localai", "google", "bing", "microsoft", "yandex".</param>
    /// <returns>An instance of <see cref="ITranslationService"/> corresponding to the specified service type.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported service type is specified.</exception>
    ITranslationService CreateTranslationService(string serviceType);

    /// <summary>
    /// Builds the ordered list of translation services from a list of provider names. Unknown or unconstructable
    /// entries are skipped with a warning, so a single bad entry doesn't prevent the list from being usable.
    /// </summary>
    /// <param name="serviceTypes">Ordered provider names. Position 0 is the primary; the rest are fallbacks.</param>
    /// <returns>Ordered list of (name, service) entries; empty if no entry could be constructed.</returns>
    IReadOnlyList<TranslationServiceEntry> CreateTranslationServices(IReadOnlyList<string> serviceTypes);
}