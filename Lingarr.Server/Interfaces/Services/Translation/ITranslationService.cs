using Lingarr.Server.Exceptions;

namespace Lingarr.Server.Interfaces.Services.Translation;

public interface ITranslationService
{
    /// <summary>
    /// Translates the given text from the source language to the target language.
    /// </summary>
    /// <param name="text">The text to be translated.</param>
    /// <param name="sourceLanguage">The language code of the source text.</param>
    /// <param name="targetLanguage">The language code of the desired translation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the translated text.</returns>
    /// <exception cref="ArgumentException">Thrown when the input parameters are invalid or empty.</exception>
    /// <exception cref="TranslationException">Thrown when an error occurs during the translation process.</exception>
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
}