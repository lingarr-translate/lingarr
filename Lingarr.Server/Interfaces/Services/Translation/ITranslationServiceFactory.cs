namespace Lingarr.Server.Interfaces.Services.Translation;

public interface ITranslationServiceFactory
{
    /// <summary>
    /// Creates and returns an instance of a translation service based on the specified service type.
    /// </summary>
    /// <param name="serviceType">The type of translation service to create. Supported values are "libretranslate" and "deepl" (case-insensitive).</param>
    /// <returns>An instance of <see cref="ITranslationService"/> corresponding to the specified service type.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported service type is specified.</exception>
    ITranslationService CreateTranslationService(string serviceType);
}