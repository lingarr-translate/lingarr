using Lingarr.Server.Exceptions;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for translating subtitles.
/// </summary>
public interface ITranslateService
{
    /// <summary>
    /// Asynchronously translates the content of a subtitle file into a specified target language and saves the translated subtitles to a new file.
    /// </summary>
    /// <param name="subtitlePath">The path to the subtitle file (.srt) to be translated.</param>
    /// <param name="targetLanguage">The language code (e.g., "en", "es") for the target language into which the subtitles should be translated.</param>
    /// <returns>
    /// A task result that contains a <see cref="List{SubtitleItem}"/> with the translated subtitles.
    /// </returns>
    /// <exception cref="TranslationException">Thrown if the translation process fails or the translation service returns an error.</exception>
    Task<List<SubtitleItem>> TranslateAsync(string subtitlePath, string targetLanguage);
}