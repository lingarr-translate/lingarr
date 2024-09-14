using System.Text.Json;

namespace Lingarr.Server.Models;

public class SonarrSettings
{
    public string? Url { get; set; } = string.Empty;
    public string? ApiKey { get; set; } = string.Empty;
}