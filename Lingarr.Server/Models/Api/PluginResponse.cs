using Lingarr.Contracts.Plugins;

namespace Lingarr.Server.Models.Api;

public sealed class PluginResponse
{
    public required string Provider { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required bool IsBuiltIn { get; init; }
    public string? SourceFile { get; init; }
    public required IReadOnlyList<PluginSettingField> Settings { get; init; }
    public required bool HasRequestTemplate { get; init; }
}
