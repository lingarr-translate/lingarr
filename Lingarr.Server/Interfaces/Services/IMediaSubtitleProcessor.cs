using Lingarr.Core.Enum;
using Lingarr.Core.Interfaces;

namespace Lingarr.Server.Interfaces.Services;

public interface IMediaSubtitleProcessor
{
    /// <summary>
    /// Processes the subtitles for a given media item.
    /// </summary>
    /// <param name="media">The media item to process subtitles for.</param>
    /// <param name="mediaType">The type of the media (e.g., Movie, Episode).</param>
    /// <returns>
    /// A boolean indicating whether new subtitle processing was initiated.
    /// Returns true if new translations were requested, false if no processing was needed or possible.
    /// </returns>
    Task<bool> ProcessMedia(IMedia media, MediaType mediaType);
}