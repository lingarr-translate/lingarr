using Lingarr.Contracts.Translation;
using Lingarr.Core.Entities;

namespace Lingarr.Server.Interfaces.Services.Translation;

public readonly record struct TranslationServiceEntry(string Name, ITranslationService Service, IBatchTranslationService? BatchService);

public interface ITranslationServiceFactory
{
    /// <summary>
    /// Creates the translation service of an instance, which reads its provider settings from that instance.
    /// Throws an ArgumentException when the provider is not supported or not loaded.
    /// </summary>
    ITranslationService CreateTranslationService(TranslationServiceInstance instance);

    /// <summary>
    /// Builds the ordered list of translation services from the configured instances, the first being the primary.
    /// Instances whose provider is unknown are skipped with a warning.
    /// </summary>
    IReadOnlyList<TranslationServiceEntry> CreateTranslationServices(IReadOnlyList<TranslationServiceInstance> instances);
}
