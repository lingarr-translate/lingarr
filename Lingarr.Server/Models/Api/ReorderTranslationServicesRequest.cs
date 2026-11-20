namespace Lingarr.Server.Models.Api;

/// <summary>
/// Represents the new cascade order as the ids of all translation service instances.
/// </summary>
public sealed class ReorderTranslationServicesRequest
{
    public required List<int> Ids { get; init; }
}
