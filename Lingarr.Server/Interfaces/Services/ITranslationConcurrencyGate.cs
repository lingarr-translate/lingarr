namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Caps the number of translations that can run concurrently across every code path
/// (Hangfire jobs, HTTP API, etc.). Sized from the MAX_CONCURRENT_JOBS environment variable.
/// </summary>
public interface ITranslationConcurrencyGate
{
    /// <summary>
    /// Waits until a translation slot is available, then returns a disposable that releases
    /// the slot when disposed. Throws <see cref="OperationCanceledException"/> if the token
    /// is cancelled while waiting.
    /// </summary>
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}
