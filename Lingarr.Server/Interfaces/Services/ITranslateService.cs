using Lingarr.Server.Exceptions;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for translating subtitles.
/// </summary>
public interface ITranslateService
{
    /// <summary>
    /// Asynchronously translates the content of a subtitle file into a specified target language and saves the translated subtitles to a new file.
    /// </summary>
    /// <param name="jobId">Id of the translation request.</param>
    /// <param name="subtitlePath">The path to the subtitle file (.srt) to be translated.</param>
    /// <param name="targetLanguage">The language code (e.g., "en", "es") for the target language into which the subtitles should be translated.</param>
    /// <param name="progressService">Progress service that is used to update the current progress.</param>
    /// <returns>
    /// A task result that contains a <see cref="List{SubtitleItem}"/> with the translated subtitles.
    /// </returns>
    /// <exception cref="TranslationException">Thrown if the translation process fails or the translation service returns an error.</exception>
    Task<List<SubtitleItem>> TranslateAsync(string jobId ,string subtitlePath, string targetLanguage, ProgressService progressService);
}