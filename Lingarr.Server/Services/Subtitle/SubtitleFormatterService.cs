using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Subtitle;

public class SubtitleFormatterService : ISubtitleFormatterService
{
    // Lines that begin with a vector drawing prefix — command letter followed
    // by at least two coord/command tokens. Catches both whole-string vector
    // paths ("m 0 0 l 100 100") and lines where drawing data leaks alongside
    // a short trailing token ("m 0 0 ka", "m 0 0 text without close"). Based
    // on @rowanfuchs's multi-command correction with a (?=\s|$) lookahead
    // that allows trailing content while still demanding a well-formed prefix.
    private static readonly Regex VectorPrefixPattern = new(
        @"^[mlcbsnMLCBSN](?:\s+(?:[mlcbsnMLCBSN]|[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)){2,}(?=\s|$)",
        RegexOptions.Compiled);

    // ASS drawing-mode blocks {\pN}...{\pM} — stripped before generic tag removal.
    private static readonly Regex DrawingTagPattern = new(
        @"\\[pP][0-9].*?\\[pP][0-9]",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <inheritdoc />
    public static string RemoveMarkup(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        // Strip ASS drawing-mode blocks {\pN}...{\p0} before generic tag removal
        string stripped = DrawingTagPattern.Replace(input, string.Empty);

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

        // Drop lines that begin with a vector drawing prefix.
        if (VectorPrefixPattern.IsMatch(result)) {
            return string.Empty;
        }

        return result;
    }
}
