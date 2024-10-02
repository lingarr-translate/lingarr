using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;

namespace Lingarr.Server.Services.Translation;

public abstract class TranslationServiceBase : ITranslationService
{
    protected readonly ISettingService _settings;
    protected readonly ILogger _logger;

    protected TranslationServiceBase(ISettingService settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public abstract Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
}