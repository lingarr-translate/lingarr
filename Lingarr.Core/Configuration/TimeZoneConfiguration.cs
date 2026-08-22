namespace Lingarr.Core.Configuration;

public static class TimeZoneConfiguration
{
    private static readonly Lazy<(TimeZoneInfo TimeZone, string? Warning)> Resolved = new(Resolve);

    public static TimeZoneInfo Current => Resolved.Value.TimeZone;

    public static string? ResolutionWarning => Resolved.Value.Warning;

    /// <summary>
    /// The current time zone as an IANA identifier, which is the form the browser understands.
    /// </summary>
    public static string IanaId => Current.HasIanaId
        ? Current.Id
        : TimeZoneInfo.TryConvertWindowsIdToIanaId(Current.Id, out var ianaId)
            ? ianaId
            : "UTC";

    private static (TimeZoneInfo TimeZone, string? Warning) Resolve()
    {
        var timeZone = Environment.GetEnvironmentVariable("TZ");
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return (TimeZoneInfo.Utc,
                "TZ is not set, falling back to UTC. Set TZ to your local time zone so schedules and displayed times match.");
        }

        try
        {
            return (TimeZoneInfo.FindSystemTimeZoneById(timeZone), null);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return (TimeZoneInfo.Utc, $"TZ is set to '{timeZone}' which is not a known time zone, falling back to UTC.");
        }
    }
}
