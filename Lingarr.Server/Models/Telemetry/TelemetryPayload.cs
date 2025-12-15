namespace Lingarr.Server.Models.Telemetry;

public class TelemetryPayload
{
    public required string Version { get; set; }
    public required string ReportDate { get; set; }
    public string? Platform { get; set; }
    public required TelemetryMetrics Metrics { get; set; }
}

public class TelemetryMetrics
{
    public long FilesTranslated { get; set; }
    public long LinesTranslated { get; set; }
    public long CharactersTranslated { get; set; }
    public Dictionary<string, int> ServiceUsage { get; set; } = new();
    public Dictionary<string, int> LanguagePairs { get; set; } = new();
    public Dictionary<string, int> MediaTypeUsage { get; set; } = new();
    public Dictionary<string, int> ModelUsage { get; set; } = new();
}
