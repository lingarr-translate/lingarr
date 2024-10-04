
using DeepL;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Translation;

public class DeepLTranslationService : TranslationServiceBase
{

    public DeepLTranslationService(
        ISettingService settings,
        ILogger<LibreTranslateService> logger) : base(settings, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var authKey = await _settings.GetSetting("deepl_api_key");
        if (string.IsNullOrWhiteSpace(authKey))
        {
            throw new TranslationException("Translation using DeepL failed, please validate the Api key.");
        }
        
        var translator = new Translator(authKey, new TranslatorOptions
        {
            MaximumNetworkRetries = 3,
            PerRetryConnectionTimeout = TimeSpan.FromSeconds(10)
        });
        var usage = await translator.GetUsageAsync();
        if (usage.AnyLimitReached)
        {
            throw new TranslationException("Translation using DeepL failed, usage limit reached.");
        }
        
        try
        {
            var result = await translator.TranslateTextAsync(
                text,
                sourceLanguage,
                targetLanguage
            );

            return result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepL translation failed");
            throw new TranslationException("Translation using DeepL failed.");
        }
    }
}