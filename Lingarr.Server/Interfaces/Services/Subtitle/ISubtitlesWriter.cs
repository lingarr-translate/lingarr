using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services.Subtitle;

/// <summary>
/// Interface specifying the required method for a SubWriter
/// </summary>
public interface ISubtitlesWriter
{
    /// <summary>
    /// Write a list of subtitle items to a stream in the SubRip (SRT) format synchronously 
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="subtitleItems">The subtitle items to write</param>
    void WriteStream(Stream stream, IEnumerable<SubtitleItem> subtitleItems);
    
    /// <summary>
    /// Write a list of subtitle items to a stream in the SubRip (SRT) format asynchronously 
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="subtitleItems">The subtitle items to write</param>
    Task WriteStreamAsync(Stream stream, IEnumerable<SubtitleItem> subtitleItems);
}