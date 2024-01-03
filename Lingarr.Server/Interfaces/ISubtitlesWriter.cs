using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces
{
    /// <summary>
    /// Interface specifying the required method for a SubWriter
    /// </summary>
    public interface ISubtitlesWriter
    {
        /// <summary>
        /// Writes a list of SubtitleItems into a stream 
        /// </summary>
        /// <param name="stream">the stream to write to</param>
        /// <param name="subtitleItems">the SubtitleItems to write</param>
        void WriteStream(Stream stream, IEnumerable<SubtitleItem> subtitleItems);

        /// <summary>
        /// Asynchronously writes a list of SubtitleItems into a stream 
        /// </summary>
        /// <param name="stream">the stream to write to</param>
        /// <param name="subtitleItems">the SubtitleItems to write</param>
        Task WriteStreamAsync(Stream stream, IEnumerable<SubtitleItem> subtitleItems);
    }
}
