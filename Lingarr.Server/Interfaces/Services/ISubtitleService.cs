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
    Task WriteSubtitles(string filePath, List<SubtitleItem> subtitles);
    
    /// <summary>
    /// Creates a new file path for a subtitle file with a specified target language.
    /// </summary>
    /// <param name="originalPath">The original path of the subtitle file.</param>
    /// <param name="targetLanguage">The target language code to be added to the file name.</param>
    /// <returns>A new file path with the target language code inserted before the .srt extension.</returns>
    string CreateFilePath(string originalPath, string targetLanguage);
    
    /// <summary>
    /// Adjusts subtitle timings to prevent overlaps and ensure optimal duration based on content length.
    /// </summary>
    /// <param name="subtitles">The list of subtitle items to process</param>
    /// <returns>The modified list of subtitles with fixed timings</returns>
    List<SubtitleItem> FixOverlappingSubtitles(List<SubtitleItem> subtitles);
}