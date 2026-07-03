namespace Lingarr.Contracts.Plugins;

/// <summary>
/// Describes one field a plugin needs from the user.
/// The settings UI renders a form from the manifest and persists values through the standard settings store.
/// </summary>
public sealed class PluginSettingField
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required PluginSettingType Type { get; init; }
    public bool Required { get; init; }
    public string? Default { get; init; }
    public string? Description { get; init; }
    public string? OptionsEndpoint { get; init; }
    public int? MinLength { get; init; }
    public string? ValidationErrorMessage { get; init; }
}
