using System.Globalization;

namespace Lingarr.Core.Helpers;

/// <summary>
/// Converts timestamps from outside Lingarr to UTC.
/// </summary>
public static class UtcDateTime
{
    /// <summary>
    /// Parses a timestamp from an upstream service as UTC.
    /// </summary>
    public static DateTimeOffset Parse(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    /// <summary>
    /// Parses a timestamp stored as a setting as UTC.
    /// </summary>
    public static bool TryParse(string? value, out DateTimeOffset parsed)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed);
    }
}
