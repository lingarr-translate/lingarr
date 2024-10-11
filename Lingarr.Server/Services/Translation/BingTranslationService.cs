using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Translation;

public class BingTranslationService : TranslationServiceBase
{
    private readonly BingTranslator _translator;

    public BingTranslationService(
        ISettingService settings,
        BingTranslator translator,
        ILogger<BingTranslationService> logger) : base(settings, logger)
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