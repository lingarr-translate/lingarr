using System.Net;
using GTranslate.Translators;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class GTranslatorService<T> : BaseLanguageService where T : ITranslator
{
    private readonly T _translator;

    public GTranslatorService(
        T translator,
        string languageFilePath,
        ISettingService settings,
        ILogger logger) : base(settings, logger, languageFilePath)
    {
        _translator = translator;
    }
    
    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text, 
        string sourceLanguage, 
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);
        
        int maxRetries = 5;
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(32);
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await _translator.TranslateAsync(
                    text, 
                    targetLanguage, 
                    sourceLanguage)
                    .WaitAsync(linked.Token)
                    .ConfigureAwait(false);
                
                return result.Translation;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for text: {Text}", text);
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                _logger.LogWarning(
                    "429 Too Many Requests. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    delay, attempt, maxRetries);

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, maxDelay.Ticks));
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