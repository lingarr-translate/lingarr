using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Translation;

public class YandexTranslationService : TranslationServiceBase
{
    private readonly YandexTranslator _translator;

    public YandexTranslationService(
        ISettingService settings,
        YandexTranslator translator,
        ILogger<YandexTranslationService> logger) : base(settings, logger)
    {
        _translator = translator;
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var result = await _translator.TranslateAsync(text, targetLanguage, sourceLanguage);
        return result.Translation;
    }
}