namespace Lingarr.Contracts.Translation;

/// <summary>
/// HTTP timeout and retry-policy values shared by translation providers.
/// </summary>
public sealed record TranslationHttpSettings(
    TimeSpan Timeout,
    int MaxRetries,
    TimeSpan RetryDelay,
    int RetryDelayMultiplier);
