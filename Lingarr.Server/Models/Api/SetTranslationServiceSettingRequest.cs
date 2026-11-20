namespace Lingarr.Server.Models.Api;

/// <summary>
/// Represents one setting value for a translation service instance.
/// </summary>
public sealed class SetTranslationServiceSettingRequest
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}
