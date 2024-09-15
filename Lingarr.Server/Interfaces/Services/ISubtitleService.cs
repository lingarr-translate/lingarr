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
    Task<List<Subtitles>> Collect(string path);
}