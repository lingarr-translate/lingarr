using System.Text;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services.Subtitle;

/// <summary>
/// Interface specifying the required method for a SubParser.
/// </summary>
public interface ISubRipParser
{
    /// <summary>
    /// Parses a stream of .srt subtitles and returns a list of subtitle items.
    /// </summary>
    /// <param name="subtitleStream">The stream containing .srt subtitles.</param>
    /// <param name="encoding">The encoding of the stream.</param>
    /// <returns>A list of <see cref="SubtitleItem"/> objects.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the stream is not seekable or readable.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the stream is not in a valid Srt format.
    /// </exception>
    List<SubtitleItem> ParseStream(Stream stream, Encoding encoding);
}