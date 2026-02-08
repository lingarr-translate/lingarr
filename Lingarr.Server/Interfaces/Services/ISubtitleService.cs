using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for collecting subtitle files.
/// </summary>
public interface ISubtitleService
{
    /// <summary>
    /// Collects subtitle files from a specified directory path and organizes them into a list of <see cref="Subtitles"/> objects.
    /// </summary>
    /// <param name="path">The relative path from the media root directory where subtitle files (.srt) are located. The path should start with a forward slash ("/").</param>
    /// <returns>
    /// A <see cref="List{Subtitles}"/> containing the collected subtitle files. Each <see cref="Subtitles"/> object includes the file path, 
    /// file name (excluding language code), and detected language code extracted from the file name. If the language code cannot be determined, 
    /// it is set to "unknown".
    /// </returns>
    Task<List<Subtitles>> GetAllSubtitles(string path);

    /// <summary>
    /// Reads and parses a subtitle file.
    /// </summary>
    /// <param name="filePath">The path to the subtitle file to be read.</param>
    /// <returns>A list of SubtitleItem objects representing the parsed subtitles.</returns>
    Task<List<SubtitleItem>> ReadSubtitles(string filePath);

    /// <summary>
    /// Writes a list of subtitle items to a file.
    /// </summary>
    /// <param name="filePath">The path where the subtitle file will be written.</param>
    /// <param name="subtitles">The list of SubtitleItem objects to be written to the file.</param>
    /// <param name="stripSubtitleFormatting">Boolean used for indicating that styles need to be stripped from the subtitle</param>
    Task WriteSubtitles(string filePath, List<SubtitleItem> subtitles, bool stripSubtitleFormatting);

    /// <summary>
    /// Creates a new file path for a subtitle file with a specified target language.
    /// </summary>
    /// <param name="originalPath">The original path of the subtitle file.</param>
    /// <param name="targetLanguage">The target language code to be added to the file name.</param>
    /// <param name="subtitleTag">Subtitle tag to be added to the file path</param>
    /// <returns>A new file path with the target language code inserted before the .srt extension.</returns>
    string CreateFilePath(string originalPath, string targetLanguage, string subtitleTag);
    
    /// <summary>
    /// Adjusts subtitle timings to prevent overlaps and ensure optimal duration based on content length.
    /// </summary>
    /// <param name="subtitles">The list of subtitle items to process</param>
    /// <returns>The modified list of subtitles with fixed timings</returns>
    List<SubtitleItem> FixOverlappingSubtitles(List<SubtitleItem> subtitles);

    /// <summary>
    /// Adds an introductory subtitle at the beginning that identifies the translation service used.
    /// The intro duration is automatically adjusted to avoid overlapping with existing subtitles.
    /// </summary>
    /// <param name="serviceType">The translation service type (e.g., "openai", "google").</param>
    /// <param name="translatedSubtitles">The subtitle list to prepend the intro to.</param>
    /// <param name="translationService">The service instance used to extract model name if available.</param>
    void AddTranslatorInfo(string serviceType, List<SubtitleItem> translatedSubtitles,
        ITranslationService translationService);
    
    
    bool ValidateSubtitle(
        string filePath,
        SubtitleValidationOptions validationOptions);

    /// <summary>
    /// Gets all subtitles from the given path that match the specified file name.
    /// </summary>
    /// <param name="path">The directory path to search for subtitle files.</param>
    /// <param name="fileName">The media file name to match against subtitle file names.</param>
    /// <returns>A list of matching subtitle files.</returns>
    Task<List<Subtitles>> GetSubtitles(string path, string fileName);

    /// <summary>
    /// Selects the source subtitle from matching subtitles by finding the first available source language
    /// and selecting the appropriate subtitle file, respecting the ignoreCaptions setting.
    /// </summary>
    /// <param name="matchingSubtitles">The list of subtitle files to search.</param>
    /// <param name="sourceCodes">The set of acceptable source language codes.</param>
    /// <param name="ignoreCaptions">The ignoreCaptions setting value.</param>
    /// <returns>A <see cref="SelectedSourceSubtitle"/> if a source subtitle was found, null otherwise.</returns>
    SelectedSourceSubtitle? SelectSourceSubtitle(List<Subtitles> matchingSubtitles, HashSet<string> sourceCodes, string ignoreCaptions);
}