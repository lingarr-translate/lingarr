using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services;

/// <inheritdoc />
public sealed class TranslationConcurrencyGate : ITranslationConcurrencyGate, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<TranslationConcurrencyGate> _logger;

    public TranslationConcurrencyGate(ILogger<TranslationConcurrencyGate> logger)
    {
        _logger = logger;

        var maxConcurrent =
            int.TryParse(Environment.GetEnvironmentVariable("MAX_CONCURRENT_JOBS"), out var parsed) && parsed > 0
                ? parsed
                : 1;

        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        _logger.LogInformation("Translation concurrency gate initialised with {MaxConcurrent} slot(s)", maxConcurrent);
    }

    /// <inheritdoc />
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        if (_semaphore.CurrentCount == 0)
        {
            _logger.LogDebug("Translation concurrency gate is full, waiting for an available slot");
        }

        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _released;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _semaphore.Release();
            }
        }
    }
}
