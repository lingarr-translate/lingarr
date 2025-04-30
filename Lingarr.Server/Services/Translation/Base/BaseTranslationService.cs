using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

public abstract class BaseTranslationService : ITranslationService
{
    protected readonly ISettingService _settings;
    protected readonly ILogger _logger;

    protected BaseTranslationService(ISettingService settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public abstract Task<string> TranslateAsync(
        string text, 
        string sourceLanguage, 
        string targetLanguage,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<List<SourceLanguage>> GetLanguages();
    
    /// <inheritdoc />
    public abstract Task<ModelsResponse> GetModels();
}