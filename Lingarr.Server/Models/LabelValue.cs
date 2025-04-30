namespace Lingarr.Server.Models;

/// <summary>
/// Represents a label-value pair for dropdown/select components in the UI
/// </summary>
public class LabelValue
{
    /// <summary>
    /// Display text
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Internal value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}