using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Subtitle;

public class SubtitleFormatterService : ISubtitleFormatterService
{
    private static readonly Regex BareVectorPattern = new(
        @"^[mlcbspMLCBSP](?:\s+[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)+$",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public static string RemoveMarkup(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        // Strip ASS drawing-mode tags {\pN}...{\p0} before other processing.
        // These wrap SVG vector commands; without stripping, bare paths remain after tag removal.
        string stripped = Regex.Replace(input, @"\\p[0-9].*?\\p0", string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Remove SSA/ASS style tags: {\...}
        stripped = Regex.Replace(stripped, @"\{.*?\}", string.Empty);

        // Remove HTML-style tags: <...>
        stripped = Regex.Replace(stripped, @"<.*?>", string.Empty);

        // Replace SSA line breaks and hard spaces with spaces
        stripped = stripped.Replace("\\N", " ").Replace("\\n", " ").Replace("\\h", " ");

        // Replace tab characters (escaped or literal)
        stripped = stripped.Replace("\\t", " ").Replace("\t", " ");

        // Collapse multiple whitespace into a single space
        stripped = Regex.Replace(stripped, @"\s{2,}", " ");

        var result = stripped.Trim();

        // Skip pure SVG vector paths (bare vector commands with no readable text)
        if (BareVectorPattern.IsMatch(result)) {
            return string.Empty;
        }

        return result;
    }
}