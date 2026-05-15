using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Subtitle;

public class SubtitleFormatterService : ISubtitleFormatterService
{
    // Matches ASS drawing blocks: {\p1} ... {\p0} or {\p2}... etc. (including scale variants)
    // Non-greedy, case-insensitive, handles the common braced form used in fansubs.
    private static readonly Regex DrawingBlockPattern = new(
        @"\{\\p[0-9]\}.*?\{\\p[0-9]\}",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Detects a line that consists primarily of ASS vector drawing commands after markup removal.
    // Matches lines starting with m, l, b, c, s, n (move/line/bezier/close/spline) followed by coordinate data.
    private static readonly Regex PureDrawingLinePattern = new(
        @"^\s*[mlbcsnMLBCSN](?:\s+[0-9a-fA-FxX.-]+){3,}\s*$",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public static string RemoveMarkup(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        // 1. Strip ASS/SSA drawing blocks first ({\p1}path data{\p0})
        string cleaned = DrawingBlockPattern.Replace(input, string.Empty);

        // 2. Remove remaining SSA/ASS style tags: {\...}
        cleaned = Regex.Replace(cleaned, @"\{.*?\}", string.Empty);

        // 3. Remove HTML-style tags: <...>
        cleaned = Regex.Replace(cleaned, @"<.*?>", string.Empty);

        // 4. Replace SSA line breaks and hard spaces with regular spaces
        cleaned = cleaned.Replace("\\N", " ").Replace("\\n", " ").Replace("\\h", " ");

        // 5. Replace tab characters (escaped or literal)
        cleaned = cleaned.Replace("\\t", " ").Replace("\t", " ");

        // 6. Collapse multiple whitespace into a single space
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");

        string result = cleaned.Trim();

        // 7. Final safety: if what remains is clearly a vector drawing command sequence, drop it entirely.
        // This catches cases where drawing data leaked outside of {\p...} tags (some fansub styles do this).
        if (PureDrawingLinePattern.IsMatch(result))
        {
            return string.Empty;
        }

        return result;
    }
}