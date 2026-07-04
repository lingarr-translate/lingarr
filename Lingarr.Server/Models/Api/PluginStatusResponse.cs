namespace Lingarr.Server.Models.Api;

public sealed class PluginStatusResponse
{
    public required string Provider { get; init; }
    public required bool Configured { get; init; }
    public required IReadOnlyList<string> MissingFields { get; init; }
}
