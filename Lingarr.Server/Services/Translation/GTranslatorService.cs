using System.Net;
using GTranslate.Translators;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Lingarr.Server.Services.Translation;

public class GTranslatorService<T> : BaseLanguageService where T : ITranslator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private T? _translator;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

    /// <inheritdoc />
    public override string? ModelName => null;

    public GTranslatorService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        string languageFilePath,
        ISettingService settings,
        ILogger logger,
        LanguageCodeService languageCodeService) : base(settings, logger, languageCodeService, languageFilePath)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier,
                SettingKeys.Translation.RequestTimeout
            ]);

            _maxRetries = int.TryParse(settings[SettingKeys.Translation.MaxRetries], out var maxRetries)
                ? maxRetries
                : 3;

            var retryDelaySeconds = int.TryParse(settings[SettingKeys.Translation.RetryDelay], out var delaySeconds)
                ? delaySeconds
                : 2;
            _retryDelay = TimeSpan.FromSeconds(retryDelaySeconds);

            _retryDelayMultiplier = int.TryParse(settings[SettingKeys.Translation.RetryDelayMultiplier], out var multiplier)
                ? multiplier
                : 2;

            var requestTimeoutMinutes = int.TryParse(settings[SettingKeys.Translation.RequestTimeout], out var requestTimeout) && requestTimeout > 0
                ? requestTimeout
                : 5;

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(requestTimeoutMinutes);

            _translator ??= ActivatorUtilities.CreateInstance<T>(_serviceProvider, httpClient);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken)
    {
        await InitializeAsync();

        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);

        var delay = _retryDelay;
        for (var attempt = 1; attempt <= _maxRetries + 1; attempt++)
        {
            try
            {
                var result = await _translator!.TranslateAsync(
                        text,
                        targetLanguage,
                        sourceLanguage)
                    .WaitAsync(linked.Token)
                    .ConfigureAwait(false);

                return result.Translation;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            {
                if (attempt > _maxRetries)
                {
                    _logger.LogError(ex, "Max retries exhausted ({StatusCode}) for text: {Text}", ex.StatusCode, text);
                    throw new TranslationException($"Retry limit reached after {ex.StatusCode}.", ex);
                }

                _logger.LogWarning(
                    "{ServiceName} received {StatusCode}. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    "GTranslator", ex.StatusCode, delay, attempt, _maxRetries);

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during translation attempt {Attempt}", attempt);
                throw new TranslationException("Unexpected error occurred during translation.", ex);
            }
        }

        throw new TranslationException("Translation failed after maximum retry attempts.");
    }
}
