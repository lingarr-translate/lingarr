namespace Lingarr.Server.Models.Api;

/// <summary>
/// Represents a request to rename a translation service instance.
/// </summary>
public sealed class UpdateTranslationServiceRequest
{
    public required string Name { get; init; }
}
