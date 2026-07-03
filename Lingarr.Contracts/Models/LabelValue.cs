namespace Lingarr.Contracts.Models;

/// <summary>
/// Label/value pair used by dropdown and select controls in the settings UI.
/// </summary>
public class LabelValue
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
