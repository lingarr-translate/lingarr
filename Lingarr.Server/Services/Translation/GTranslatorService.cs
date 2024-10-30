using GTranslate.Translators;
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
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var result = await _translator.TranslateAsync(text, targetLanguage, sourceLanguage);
        return result.Translation;
    }
}