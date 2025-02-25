using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services.Subtitle;

public class SrtParser : ISubtitleParser
{
    private readonly string[] _timeDelimiters = { "-->", "- >", "->" };
    private static readonly Regex TimeCodeRegex = new(@"([0-9]+):([0-9]+):([0-9]+)(?:[,\.]([0-9]+))?", RegexOptions.Compiled);
    private const int MinimumSubtitleBlockSize = 2;

    /// <inheritdoc />
    public List<SubtitleItem> ParseStream(Stream subtitleStream, Encoding encoding)
    {
        try
        {
            ValidateStream(subtitleStream);
            using var reader = new StreamReader(subtitleStream, encoding, true);
            var subtitles = ParseSubtitles(reader).ToList();
            
            if (subtitles.Count == 0)
            {
                throw new FormatException("No valid subtitles found in the stream");
            }

            return subtitles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing subtitle file: {ex.Message}");
            return new List<SubtitleItem>();
        }
    }

    /// <summary>
    /// Parses subtitle lines into subtitle items.
    /// </summary>
    /// <param name="reader">The text reader to read subtitle lines from.</param>
    /// <returns>An enumerable collection of subtitle items.</returns>
    private IEnumerable<SubtitleItem> ParseSubtitles(TextReader reader)
    {
        var currentBlock = new List<string>();
        string? line;

        while ((line = ReadNonEmptyLine(reader)) != null)
        {
            if (IsBlockSeparator(line, currentBlock, reader))
            {
                if (TryParseBlock(currentBlock, out var subtitle))
                {
                    yield return subtitle;
                }
                currentBlock.Clear();
            }
            currentBlock.Add(line);
        }

        if (TryParseBlock(currentBlock, out var lastSubtitle))
        {
            yield return lastSubtitle;
        }
    }

    /// <summary>
    /// Reads the next non-empty line from the subtitle.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <returns>The next non-empty line, or null if none remain.</returns>
    private static string? ReadNonEmptyLine(TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Determines if the given line marks the start of a new subtitle block.
    /// </summary>
    /// <param name="line">The current line.</param>
    /// <param name="currentBlock">The current subtitle block being processed.</param>
    /// <param name="reader">The text reader.</param>
    /// <returns>True if the line is a block separator; otherwise, false.</returns>
    private bool IsBlockSeparator(string line, List<string> currentBlock, TextReader reader)
    {
        if (currentBlock.Count == 0)
        {
            return false;
        }
        return IsNumericLine(line) && IsNumericLine(currentBlock[0]);
    }

    /// <summary>
    /// Attempts to parse a subtitle block.
    /// </summary>
    /// <param name="block">The subtitle block to parse.</param>
    /// <param name="subtitle">The parsed subtitle item.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    private bool TryParseBlock(List<string> block, out SubtitleItem subtitle)
    {
        subtitle = new SubtitleItem();

        if (block.Count < MinimumSubtitleBlockSize || !IsNumericLine(block[0]))
        {
            return false;
        }

        if (!TryParseTimeCodes(block[1], out var start, out var end))
        {
            return false;
        }

        subtitle.Position = int.Parse(block[0]);
        subtitle.StartTime = start;
        subtitle.EndTime = end;
        ParseTextLines(block.Skip(2), subtitle);

        return subtitle.Lines.Count > 0;
    }

    /// <summary>
    /// Attempts to parse timecodes from a subtitle line.
    /// </summary>
    /// <param name="line">The subtitle line containing timecodes.</param>
    /// <param name="start">The parsed start time in milliseconds.</param>
    /// <param name="end">The parsed end time in milliseconds.</param>
    /// <returns>True if the timecodes were successfully parsed; otherwise, false.</returns>
    private bool TryParseTimeCodes(string line, out int start, out int end)
    {
        start = end = -1;
        var parts = line.Split(_timeDelimiters, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return false;
        }
        
        start = ParseTimeCode(parts[0]);
        end = ParseTimeCode(parts[1]);
        
        if (start >= 0 && end >= 0 && end < start)
        {
            (start, end) = (end, start);
        }
        
        return start >= 0 && end >= 0;
    }

    /// <summary>
    /// Parses an SRT timecode string into milliseconds.
    /// </summary>
    /// <param name="timeCode">The timecode string in HH:MM:SS,mmm format.</param>
    /// <returns>The parsed timecode in milliseconds, or -1 if parsing fails.</returns>
    private static int ParseTimeCode(string timeCode)
    {
        var match = TimeCodeRegex.Match(timeCode.Trim());
        if (!match.Success) return -1;

        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var seconds = int.Parse(match.Groups[3].Value);
        var milliseconds = match.Groups[4].Success 
            ? int.Parse(match.Groups[4].Value.PadRight(3, '0').Substring(0, 3))
            : 0;

        if (minutes >= 60 || seconds >= 60)
        {
            return -1;
        }

        return hours * 3600000 + minutes * 60000 + seconds * 1000 + milliseconds;
    }

    /// <summary>
    /// Parses subtitle text lines and removes markup.
    /// </summary>
    /// <param name="lines">The subtitle text lines.</param>
    /// <param name="subtitle">The subtitle item to populate.</param>
    private static void ParseTextLines(IEnumerable<string> lines, SubtitleItem subtitle)
    {
        foreach (var line in lines)
        {
            var cleanedLine = line.Trim();
            if (string.IsNullOrEmpty(cleanedLine))
            {
                continue;
            }
            
            subtitle.Lines.Add(cleanedLine);
            subtitle.PlaintextLines.Add(RemoveMarkup(cleanedLine));
        }
    }

    /// <summary>
    /// Removes markup tags from a subtitle line.
    /// </summary>
    /// <param name="input">The subtitle line with potential markup.</param>
    /// <returns>The cleaned subtitle text without markup.</returns>
    private static string RemoveMarkup(string input)
    {
        return Regex.Replace(input, @"\{.*?\}|<.*?>", string.Empty).Trim();
    }

    /// <summary>
    /// Determines if a given line is numeric.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is numeric; otherwise, false.</returns>
    private static bool IsNumericLine(string line)
    {
        return int.TryParse(line.Trim(), out _);
    }
    
    /// <summary>
    /// Validates if the given stream is readable and seekable.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the stream is not readable or seekable.</exception>
    private static void ValidateStream(Stream stream)
    {
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("Stream must be readable and seekable");
        }

        stream.Position = 0;
    }
}