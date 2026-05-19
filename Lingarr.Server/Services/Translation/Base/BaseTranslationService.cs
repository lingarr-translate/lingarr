using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

public abstract class BaseTranslationService : ITranslationService
{
    protected readonly ISettingService _settings;
    protected readonly ILogger _logger;
    protected readonly LanguageCodeService _languageCodeService;

    protected BaseTranslationService(
        ISettingService settings,
        ILogger logger,
        LanguageCodeService languageCodeService)
    {
        _settings = settings;
        _logger = logger;
        _languageCodeService = languageCodeService;
    }

    /// <inheritdoc />
    public abstract string? ModelName { get; }

    /// <inheritdoc />
    public abstract Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<List<SourceLanguage>> GetLanguages();

    /// <inheritdoc />
    public abstract Task<ModelsResponse> GetModels();

    /// <inheritdoc />
    public virtual async Task<LanguagePair?> GetLanguagePair(
        string requestedSource,
        string requestedTarget,
        CancellationToken cancellationToken)
    {
        List<SourceLanguage> languages;
        try
        {
            languages = await GetLanguages();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Could not retrieve supported languages, treating as unsupported pair.");
            return null;
        }

        LanguagePair? bestLanguagePair = null;
        foreach (var language in languages)
        {
            var sourceMatch = _languageCodeService.GetBestMatch(requestedSource, [language.Code]);
            if (sourceMatch is null)
            {
                continue;
            }

            var targetMatch = _languageCodeService.GetBestMatch(requestedTarget, language.Targets);
            if (targetMatch is null)
            {
                continue;
            }

            var tier = sourceMatch.Tier;
            if (targetMatch.Tier > tier)
            {
                tier = targetMatch.Tier;
            }

            if (bestLanguagePair != null && tier >= bestLanguagePair.Tier)
            {
                continue;
            }

            bestLanguagePair = new LanguagePair
            {
                Source = sourceMatch.Code, 
                Target = targetMatch.Code, 
                Tier = tier
            };
            if (tier == MatchTier.Exact)
            {
                break;
            }
        }

        return bestLanguagePair;
    }
}