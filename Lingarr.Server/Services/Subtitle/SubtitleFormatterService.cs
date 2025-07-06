using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Subtitle;

public class SubtitleFormatterService : ISubtitleFormatterService
{
    /// <inheritdoc />
    public static string RemoveMarkup(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        // Remove SSA/ASS style tags: {\...}
        string cleaned = Regex.Replace(input, @"\{.*?\}", string.Empty);

        // Remove HTML-style tags: <...>
        cleaned = Regex.Replace(cleaned, @"<.*?>", string.Empty);

        // Replace SSA line breaks with spaces
        cleaned = cleaned.Replace("\\N", " ").Replace("\\n", " ");

        // Replace tab characters (escaped or literal)
        cleaned = cleaned.Replace("\\t", " ").Replace("\t", " ");

        // Optional: remove other control-like tags, e.g. [note], (custom formats)
        cleaned = Regex.Replace(cleaned, @"\[[^\]]*]", string.Empty);

        // Collapse multiple whitespace into a single space
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");

        return cleaned.Trim();
    }
}