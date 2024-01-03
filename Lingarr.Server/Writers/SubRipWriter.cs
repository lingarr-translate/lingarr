using Lingarr.Server.Interfaces;
using Lingarr.Server.Models;

namespace Lingarr.Server.Writers;

/// <summary>
/// Forked from: https://github.com/AlexPoint/SubtitlesParser
/// Writer for the .srt subrip subtitle files
/// </summary>
public class SubRipWriter : ISubtitlesWriter
{
    /// <summary>
    /// Converts a subtitle item into the lines for an SRT subtitle entry
    /// </summary>
    /// <param name="subtitleItem">The SubtitleItem to convert</param>
    /// <param name="subtitleEntryNumber">The subtitle number for the entry (increments sequentially from 1)</param>
    /// <returns>A list of strings to write as an SRT subtitle entry</returns>
    private IEnumerable<string> SubtitleItemToSubtitleEntry(SubtitleItem subtitleItem, int subtitleEntryNumber)
    {
        // take the start and end timestamps and format it as a timecode line
        string formatTimeCodeLine()
        {
            TimeSpan start = TimeSpan.FromMilliseconds(subtitleItem.StartTime);
            TimeSpan end = TimeSpan.FromMilliseconds(subtitleItem.EndTime);
            return $"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}";
        }

        List<string> lines = new List<string>();
        lines.Add(subtitleEntryNumber.ToString());
        lines.Add(formatTimeCodeLine());
        lines.AddRange(subtitleItem.Lines);
            
        return lines;
    }

    /// <summary>
    /// Write a list of subtitle items to a stream in the SubRip (SRT) format synchronously 
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="subtitleItems">The subtitle items to write</param>
    public void WriteStream(Stream stream, IEnumerable<SubtitleItem> subtitleItems)
    {
        using TextWriter writer = new StreamWriter(stream);

        List<SubtitleItem> items = subtitleItems.ToList(); // avoid multiple enumeration since we're using a for instead of foreach
        for (int index = 0; index < items.Count; index++)
        {
            SubtitleItem subtitleItem = items[index];
            IEnumerable<string> lines = SubtitleItemToSubtitleEntry(subtitleItem, subtitleItem.Position); // add one because subtitle entry numbers start at 1 instead of 0
            foreach (string line in lines)
            {
                writer.WriteLine(line);
            }

            writer.WriteLine(); // empty line between subtitle entries
        }
    }

    /// <summary>
    /// Write a list of subtitle items to a stream in the SubRip (SRT) format asynchronously 
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="subtitleItems">The subtitle items to write</param>
    public async Task WriteStreamAsync(Stream stream, IEnumerable<SubtitleItem> subtitleItems)
    {
        await using TextWriter writer = new StreamWriter(stream);

        List<SubtitleItem> items = subtitleItems.ToList(); // avoid multiple enumeration since we're using a for instead of foreach
        for (int index = 0; index < items.Count; index++)
        {
            SubtitleItem subtitleItem = items[index];
            // Create a subtitle entry
            IEnumerable<string> lines = SubtitleItemToSubtitleEntry(subtitleItem, subtitleItem.Position); // add one because subtitle entry numbers start at 1 instead of 0
            foreach (string line in lines)
            {
                await writer.WriteLineAsync(line);
            }

            await writer.WriteLineAsync(); // empty line between subtitle entries
        }
    }
}
