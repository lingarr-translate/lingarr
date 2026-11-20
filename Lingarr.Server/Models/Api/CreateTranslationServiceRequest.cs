namespace Lingarr.Server.Models.Api;

/// <summary>
/// Represents a request to add a translation service instance, the name defaults to the provider display name.
/// </summary>
public sealed class CreateTranslationServiceRequest
{
    public required string Provider { get; init; }
    public string? Name { get; init; }
}
