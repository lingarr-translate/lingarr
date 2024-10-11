using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Translation;

public class MicrosoftTranslationService : TranslationServiceBase
{
    private readonly MicrosoftTranslator _translator;

    public MicrosoftTranslationService(
        ISettingService settings,
        MicrosoftTranslator translator,
        ILogger<MicrosoftTranslationService> logger) : base(settings, logger)
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