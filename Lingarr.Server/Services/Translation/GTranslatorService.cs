using System.Linq;
using System.Net;
using GTranslate.Translators;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class GTranslatorService<T> : BaseLanguageService where T : ITranslator
{
    private const int MicrosoftTranslatorMaxCharacters = 1000;
    private readonly T _translator;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

    /// <inheritdoc />
    public override string? ModelName => null;

    public GTranslatorService(
        T translator,
        string languageFilePath,
        ISettingService settings,
        ILogger logger,
        LanguageCodeService languageCodeService) : base(settings, logger, languageCodeService, languageFilePath)
    {
        _translator = translator;
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            await _initLock.WaitAsync();
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier
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

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var segments = GetTranslationSegments(text);
        if (segments.Count > 1)
        {
            _logger.LogWarning(
                "Splitting Microsoft Translator input into {SegmentCount} chunks because the line length {Length} exceeds the {Limit}-character limit.",
                segments.Count,
                text.Length,
                MicrosoftTranslatorMaxCharacters);
        }

        var translatedSegments = new List<string>(segments.Count);
        foreach (var segment in segments)
        {
            translatedSegments.Add(await TranslateSegmentAsync(
                segment,
                sourceLanguage,
                targetLanguage,
                linked.Token));
        }

        return string.Join(" ", translatedSegments.Where(segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private async Task<string> TranslateSegmentAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var delay = _retryDelay;
        for (var attempt = 1; attempt <= _maxRetries + 1; attempt++)
        {
            try
            {
                var result = await _translator.TranslateAsync(
                        text,
                        targetLanguage,
                        sourceLanguage)
                    .WaitAsync(cancellationToken)
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

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
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

    private static List<string> GetTranslationSegments(string text)
    {
        if (!IsMicrosoftTranslator() || text.Length <= MicrosoftTranslatorMaxCharacters)
        {
            return [text];
        }

        return TranslationTextChunker.Split(text, MicrosoftTranslatorMaxCharacters);
    }

    private static bool IsMicrosoftTranslator()
    {
        return typeof(T) == typeof(MicrosoftTranslator) ||
               typeof(T).Name == nameof(MicrosoftTranslator);
    }
}
