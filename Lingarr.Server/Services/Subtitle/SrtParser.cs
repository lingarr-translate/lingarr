using System.Text.RegularExpressions;
using System.Text;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services.Subtitle;

/// <summary>
/// Forked from: https://github.com/AlexPoint/SubtitlesParser
/// Parser for the .srt subrip subtitle files
/// </summary>
public class SrtParser : ISubtitleParser
{
    private readonly string[] _delimiters = { "-->", "- >", "->" };

    /// <summary>
    /// Initializes a new instance of the <see cref="SrtParser"/> class.
    /// </summary>
    public SrtParser()
    {
    }
    
    /// <inheritdoc />
    public List<SubtitleItem> ParseStream(Stream subtitleStream, Encoding encoding)
    {
        // Ensure the stream is readable and seekable
        if (!subtitleStream.CanRead || !subtitleStream.CanSeek)
        {
            throw new ArgumentException($"Stream must be seekable and readable in a subtitles parser. " +
                                        $"Operation interrupted; isSeekable: {subtitleStream.CanSeek} - isReadable: {subtitleStream.CanSeek}");
        }

        // Seek the beginning of the stream
        subtitleStream.Position = 0;

        using var reader = new StreamReader(subtitleStream, encoding, true);

        var items = new List<SubtitleItem>();
        var subtitleParts = GetSubtitleParts(reader).ToList();

        if (!subtitleParts.Any())
        {
            throw new FormatException("Parsing as srt returned no srt part.");
        }

        foreach (var subtitlePart in subtitleParts)
        {
            var subtitleLines = subtitlePart.Split(Environment.NewLine)
                .Select(s => s.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            var item = new SubtitleItem();

            foreach (var subtitleLine in subtitleLines)
            {
                if (item.StartTime == 0 && item.EndTime == 0 &&
                    TryParseTimeCodeLine(subtitleLine, out var startTc, out var endTc))
                {
                    item.StartTime = startTc;
                    item.EndTime = endTc;
                }
                else
                {
                    if (int.TryParse(subtitleLine, out _))
                    {
                        item.Position = int.Parse(subtitleLine);
                    }
                    else
                    {
                        item.Lines.Add(subtitleLine);
                        // Strip formatting by removing anything within curly braces or angle brackets,
                        // which is how SRT styles text according to Wikipedia (https://en.wikipedia.org/wiki/SubRip#Formatting)
                        item.PlaintextLines.Add(Regex.Replace(subtitleLine, @"\{.*?\}|<.*?>", string.Empty));
                    }
                }
            }

            if ((item.StartTime != 0 || item.EndTime != 0) && item.Lines.Any())
            {
                // Parsing succeeded
                items.Add(item);
            }
        }

        if (!items.Any())
        {
            throw new ArgumentException("Stream is not in a valid Srt format");
        }

        return items;
    }


    /// <summary>
    /// Enumerates the subtitle parts in a .srt file based on the standard line break observed between them.
    /// A .srt subtitle part is in the form:
    /// 
    /// 1
    /// 00:00:20,000 --> 00:00:24,400
    /// Altocumulus clouds occur between six thousand
    /// </summary>
    /// <param name="reader">The text reader associated with the .srt file.</param>
    /// <returns>An IEnumerable(string) object containing all the subtitle parts.</returns>
    private IEnumerable<string> GetSubtitleParts(TextReader reader)
    {
        var stringBuilder = new StringBuilder();
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                // Return only if not empty
                var result = stringBuilder.ToString().TrimEnd();
                if (!string.IsNullOrEmpty(result))
                {
                    yield return result;
                }

                stringBuilder.Clear();
            }
            else
            {
                stringBuilder.AppendLine(line);
            }
        }

        if (stringBuilder.Length > 0)
        {
            yield return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// Attempts to parse a timecode line and extracts start and end timecodes.
    /// </summary>
    /// <param name="line">The timecode line to parse.</param>
    /// <param name="startTimeCode">The parsed start timecode.</param>
    /// <param name="endTimeCode">The parsed end timecode.</param>
    /// <returns>
    /// <c>true</c> if the parsing is successful; otherwise, <c>false</c>.
    /// </returns>
    private bool TryParseTimeCodeLine(string line, out int startTimeCode, out int endTimeCode)
    {
        var parts = line.Split(_delimiters, StringSplitOptions.None);

        if (parts.Length != 2)
        {
            // This is not a timecode line
            (startTimeCode, endTimeCode) = (-1, -1);
            return false;
        }
        else
        {
            (startTimeCode, endTimeCode) = (ParseTimeCode(parts[0]), ParseTimeCode(parts[1]));
            return true;
        }
    }

    /// <summary>
    /// Parses a timecode string in SRT format into milliseconds.
    /// </summary>
    /// <param name="line">The SRT timecode string.</param>
    /// <returns>
    /// The parsed timecode in milliseconds. If parsing fails, -1 is returned.
    /// </returns>
    private static int ParseTimeCode(string line)
    {
        var match = Regex.Match(line, @"[0-9]+:[0-9]+:[0-9]+([,\.][0-9]+)?");

        if (match.Success && TimeSpan.TryParse(match.Value.Replace(',', '.'), out var result))
        {
            return (int)result.TotalMilliseconds;
        }

        return -1;
    }
}