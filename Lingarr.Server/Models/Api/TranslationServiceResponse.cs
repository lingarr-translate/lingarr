namespace Lingarr.Server.Models.Api;

/// <summary>
/// Represents a configured translation service instance in the cascade.
/// </summary>
public sealed class TranslationServiceResponse
{
    public required int Id { get; init; }
    public required string Provider { get; init; }
    public required string Name { get; init; }
    public required int Position { get; init; }
}
