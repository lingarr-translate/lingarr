namespace Lingarr.Server.Services.Translation;

public static class TranslationTextChunker
{
    public static List<string> Split(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length must be positive.");
        }

        if (text.Length <= maxLength)
        {
            return [text];
        }

        var chunks = new List<string>();
        var remaining = text.Trim();

        while (remaining.Length > maxLength)
        {
            var splitIndex = FindSplitIndex(remaining, maxLength);
            var chunk = remaining[..splitIndex].TrimEnd();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            remaining = remaining[splitIndex..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(remaining))
        {
            chunks.Add(remaining.Trim());
        }

        return chunks;
    }

    private static int FindSplitIndex(string text, int maxLength)
    {
        var upperBound = Math.Min(maxLength, text.Length);
        for (var index = upperBound; index > 0; index--)
        {
            if (char.IsWhiteSpace(text[index - 1]))
            {
                return index;
            }
        }

        return upperBound;
    }
}
