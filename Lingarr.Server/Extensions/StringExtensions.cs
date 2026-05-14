using System.Text;

namespace Lingarr.Server.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Splits text into lines based on maximum line length, ensuring words are not broken.
    /// </summary>
    /// <param name="text">The text to split into lines.</param>
    /// <param name="maxLineLength">Maximum allowed length per line.</param>
    /// <returns>A list of lines that fit within the specified length.</returns>
    public static List<string> SplitIntoLines(this string text, int maxLineLength)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (var word in text.Split(' '))
        {
            if (line.Length == 0)
            {
                line.Append(word);
            }
            else if (line.Length + 1 + word.Length <= maxLineLength)
            {
                line.Append(' ').Append(word);
            }
            else
            {
                lines.Add(line.ToString());
                line.Clear();
                line.Append(word);
            }
        }
        lines.Add(line.ToString());
        return lines;
    }
}
