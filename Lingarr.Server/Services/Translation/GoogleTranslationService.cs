using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Translation;

public class GoogleTranslationService : TranslationServiceBase
{
    private readonly GoogleTranslator _translator;

    public GoogleTranslationService(
        ISettingService settings,
        GoogleTranslator translator,
        ILogger<GoogleTranslationService> logger) : base(settings, logger)
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