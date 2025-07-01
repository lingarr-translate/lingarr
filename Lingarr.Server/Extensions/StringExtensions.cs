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
        return text.Split(' ')
            .Aggregate(new List<string> { "" },
                (lines, word) =>
                {
                    var currentLine = lines.Last();

                    if (currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0) <= maxLineLength)
                    {
                        lines[^1] = currentLine.Length == 0 ? word : $"{currentLine} {word}";
                    }
                    else
                    {
                        lines.Add(word);
                    }

                    return lines;
                });
    }
}