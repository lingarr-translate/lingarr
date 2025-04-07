using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services.Subtitle;

public class SsaParser : ISubtitleParser
{
    private const string SCRIPT_INFO_SECTION = "[Script Info]";
    private const string V4_PLUS_STYLES_SECTION = "[V4+ Styles]";
    private const string V4_STYLES_SECTION = "[V4 Styles]";
    private const string EVENTS_SECTION = "[Events]";
    private const string DIALOGUE_PREFIX = "Dialogue:";
    private const string WRAP_STYLE_PREFIX = "WrapStyle:";

    public List<SubtitleItem> ParseStream(Stream ssaStream, Encoding encoding)
    {
        if (!ssaStream.CanRead || !ssaStream.CanSeek)
        {
            throw new ArgumentException("Subtitle must be seekable and readable");
        }

        // seek the beginning of the stream
        ssaStream.Position = 0;
        using var reader = new StreamReader(ssaStream, encoding, true);

        var items = new List<SubtitleItem>();
        var currentSection = string.Empty;
        var ssaFormat = new SsaFormat();
        Dictionary<string, int>? columnIndexes = null;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            // Handle section changes
            if (line.StartsWith("["))
            {
                currentSection = line;
                switch (currentSection)
                {
                    case SCRIPT_INFO_SECTION:
                        ssaFormat.ScriptInfo.Add(line);
                        break;
                    case V4_PLUS_STYLES_SECTION:
                        ssaFormat.Styles.Add(line);
                        break;
                    case V4_STYLES_SECTION:
                        ssaFormat.Styles.Add(line);
                        break;
                    case EVENTS_SECTION:
                        ssaFormat.EventsFormat.Add(line);
                        break;
                }
                continue;
            }

            // Store original section content
            switch (currentSection)
            {
                case SCRIPT_INFO_SECTION:
                    ssaFormat.ScriptInfo.Add(line);
                    if (line.StartsWith(WRAP_STYLE_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        var wrapStyleValue = line.Substring(WRAP_STYLE_PREFIX.Length).Trim();
                        if (int.TryParse(wrapStyleValue, out int wrapStyleInt))
                        {
                            ssaFormat.WrapStyle = (SsaWrapStyle)wrapStyleInt;
                        }
                    }
                    break;
                case V4_PLUS_STYLES_SECTION:
                    ssaFormat.Styles.Add(line);
                    break;
                case V4_STYLES_SECTION:
                    ssaFormat.Styles.Add(line);
                    break;
                case EVENTS_SECTION:
                    if (line.StartsWith("Format:"))
                    {
                        ssaFormat.EventsFormat.Add(line);
                        var columns = line.Substring(7).Split(',')
                            .Select(c => c.Trim())
                            .ToList();
                        columnIndexes = new Dictionary<string, int>();
                        for (var index = 0; index < columns.Count; index++)
                        {
                            columnIndexes[columns[index]] = index;
                        }
                    }
                    else if (line.StartsWith(DIALOGUE_PREFIX) && columnIndexes != null)
                    {
                        var dialogue = ParseDialogueLine(line, columnIndexes, ssaFormat);
                        if (dialogue != null)
                        {
                            dialogue.SsaFormat = ssaFormat;
                            items.Add(dialogue);
                        }
                    }
                    break;
            }
        }

        if (!items.Any())
        {
            throw new ArgumentException("No valid subtitles found in SSA format");
        }

        return items;
    }

    private List<string> SplitTextByWrapStyle(string text, SsaWrapStyle wrapStyle)
    {
        return wrapStyle switch
        {
            SsaWrapStyle.Smart or SsaWrapStyle.SmartWideLowerLine => 
                // For Smart wrapping, only \N breaks count
                text.Split(new[] { "\\N" }, StringSplitOptions.None).ToList(),
                
            SsaWrapStyle.EndOfLine => 
                // For End-of-line wrapping, only \N breaks count
                text.Split(new[] { "\\N" }, StringSplitOptions.None).ToList(),
                
            SsaWrapStyle.None => 
                // For No wrapping, both \n and \N break
                System.Text.RegularExpressions.Regex
                    .Split(text, @"\\N|\\n")
                    .ToList(),
                
            _ => new List<string> { text } // Default case
        };
    }

    /// <summary>
    /// Removes markup tags from a subtitle line.
    /// </summary>
    /// <param name="input">The subtitle line with potential markup.</param>
    /// <returns>The cleaned subtitle text without markup.</returns>
    private static string RemoveMarkup(string input)
    {
        // Remove SSA style tags like {\an8}, {\i1}, etc.
        string noSsaTags = Regex.Replace(input, @"\{.*?\}", string.Empty);
        
        // Remove HTML-style tags
        string noHtmlTags = Regex.Replace(noSsaTags, @"<.*?>", string.Empty);
        
        // Replace SSA line breaks with spaces for plaintext
        string noLineBreaks = noHtmlTags.Replace("\\N", " ").Replace("\\n", " ");
        
        return noLineBreaks.Trim();
    }

    private SubtitleItem? ParseDialogueLine(string line, Dictionary<string, int> columnIndexes, SsaFormat ssaFormat)
    {
        var dialogueParts = line.Substring(DIALOGUE_PREFIX.Length).Split(',');
        if (dialogueParts.Length >= columnIndexes.Count)
        {
            // Extract basic timing information
            var startIndex = columnIndexes["Start"];
            var endIndex = columnIndexes["End"];
            var textIndex = columnIndexes["Text"];

            var startTime = ParseSsaTimecode(dialogueParts[startIndex].Trim());
            var endTime = ParseSsaTimecode(dialogueParts[endIndex].Trim());
            var text = string.Join(",", dialogueParts.Skip(textIndex)).Trim();

            if (startTime >= 0 && endTime >= 0 && !string.IsNullOrEmpty(text))
            {
                // Split text according to wrap style
                var textLines = SplitTextByWrapStyle(text, ssaFormat.WrapStyle);
                var plaintextLines = new List<string>();
                foreach (var textLine in textLines)
                {
                    plaintextLines.Add(RemoveMarkup(textLine));
                }
                
                // Create SsaDialogue info
                var ssaDialogue = new SsaDialogue
                {
                    Marked = dialogueParts[0].Trim(),
                    Style = dialogueParts[columnIndexes["Style"]].Trim(),
                    Name = dialogueParts[columnIndexes["Name"]].Trim(),
                    MarginL = dialogueParts[columnIndexes["MarginL"]].Trim(),
                    MarginR = dialogueParts[columnIndexes["MarginR"]].Trim(),
                    MarginV = dialogueParts[columnIndexes["MarginV"]].Trim(),
                    Effect = dialogueParts[columnIndexes["Effect"]].Trim()
                };

                return new SubtitleItem
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    Lines = textLines,
                    PlaintextLines = plaintextLines,
                    SsaDialogue = ssaDialogue,
                    SsaFormat = ssaFormat
                };
            }
        }
        return null;
    }

    private int ParseSsaTimecode(string timestamp)
    {
        if (TimeSpan.TryParse(timestamp, out var timeSpan))
        {
            return (int)timeSpan.TotalMilliseconds;
        }
        return -1;
    }
}
