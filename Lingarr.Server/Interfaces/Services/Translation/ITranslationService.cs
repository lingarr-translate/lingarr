using Lingarr.Server.Exceptions;
using Lingarr.Server.Models;
using Newtonsoft.Json;

namespace Lingarr.Server.Interfaces.Services.Translation;

public interface ITranslationService
{
    /// <summary>
    /// Translates the given text from the source language to the target language.
    /// </summary>
    /// <param name="text">The text to be translated.</param>
    /// <param name="sourceLanguage">The language code of the source text.</param>
    /// <param name="targetLanguage">The language code of the desired translation.</param>
    /// <param name="contextLinesBefore">List of subtitle lines after the subtitle line</param>
    /// <param name="contextLinesAfter">List of subtitle lines after the subtitle line</param>
    /// <param name="cancellationToken">Token to cancel the translation operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the translated text.</returns>
    /// <exception cref="ArgumentException">Thrown when the input parameters are invalid or empty.</exception>
    /// <exception cref="TranslationException">Thrown when an error occurs during the translation process.</exception>
    Task<string> TranslateAsync(
        string text, 
        string sourceLanguage, 
        string targetLanguage, 
        List<string>? contextLinesBefore, 
        List<string>? contextLinesAfter, 
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves a list of available source languages and their supported target languages.
    /// </summary>
    /// <returns>A list of source languages, each containing its code, name, and list of supported target language codes</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not properly configured or initialization fails</exception>
    /// <exception cref="JsonException">Thrown when language configuration files cannot be parsed (for file-based services)</exception>
    Task<List<SourceLanguage>> GetLanguages();
    
    /// <summary>
    /// Retrieves available LLM (Large Language Model) models from AI services.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ModelsResponse"/> with information about available models.</returns>
    /// <exception cref="Exception">Thrown when an error occurs while fetching models from the AI service.</exception>
    Task<ModelsResponse> GetModels();
}