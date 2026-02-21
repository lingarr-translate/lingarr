using System.Text.Json;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

public abstract class BaseLanguageService : BaseTranslationService
{
    private readonly string _languageFilePath;
    protected readonly LanguageCodeService _languageCodeService;
    protected string? _contextPrompt;
    protected string? _contextPromptEnabled;
    protected Dictionary<string, string> _replacements;

    protected BaseLanguageService(
        ISettingService settings,
        ILogger logger,
        LanguageCodeService languageCodeService,
        string languageFilePath) : base(settings, logger)
    {
        _languageFilePath = languageFilePath;
        _languageCodeService = languageCodeService;
        _replacements = new Dictionary<string, string>();
    }
    
    protected string ReplacePlaceholders(string promptTemplate, Dictionary<string, string> replacements)
    {
        if (string.IsNullOrEmpty(promptTemplate))
            return promptTemplate;

        var result = promptTemplate;
        foreach (var replacement in replacements)
        {
            result = result.Replace($"{{{replacement.Key}}}", replacement.Value);
        }

        return result;
    }

    protected string ApplyContextIfEnabled(
        string text, 
        List<string>? contextLinesBefore, 
        List<string>? contextLinesAfter)
    {
        if (_contextPromptEnabled != "true" || string.IsNullOrEmpty(_contextPrompt))
        {
            return text;
        }

        _replacements["contextBefore"] = string.Join("\n", contextLinesBefore ?? []);
        _replacements["lineToTranslate"] = text;
        _replacements["contextAfter"] = string.Join("\n", contextLinesAfter ?? []);
        return ReplacePlaceholders(_contextPrompt, _replacements);
    }

    /// <inheritdoc />
    public override async Task<List<SourceLanguage>> GetLanguages()
    {
        _logger.LogInformation($"Retrieving |Green|{_languageFilePath}|/Green| languages");
        var sourceLanguages = await GetJson();
        
        var languageCodes = sourceLanguages.Select(l => l.Code).ToHashSet();
        return sourceLanguages
            .Select(lang => new SourceLanguage
            {
                Code = lang.Code,
                Name = lang.Name,
                Targets = languageCodes
                    .Where(code => code != lang.Code)
                    .ToList()
            })
            .ToList();
    }

    /// <summary>
    /// Reads and deserializes the language configuration from a JSON file.
    /// </summary>
    /// <returns>A list of language configurations from the JSON file</returns>
    /// <exception cref="JsonException">Thrown when deserialization of the JSON file fails</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read</exception>
    private async Task<List<JsonLanguage>> GetJson()
    {
        string jsonContent = await File.ReadAllTextAsync(_languageFilePath);
        var sourceLanguages = JsonSerializer.Deserialize<List<JsonLanguage>>(jsonContent);
        if (sourceLanguages == null)
        {
            throw new JsonException($"Failed to deserialize {_languageFilePath}");
        }

        return sourceLanguages;
    }

    /// <inheritdoc />
    public override async Task<ModelsResponse> GetModels()
    {
        return await Task.FromResult(new ModelsResponse());
    }
    
    protected void SetLanguageReplacements(string sourceLanguage, string targetLanguage, string languageCodeFormatSetting)
    {
        var useLanguageCodes = languageCodeFormatSetting == "true";
        _replacements = new Dictionary<string, string>
        {
            ["sourceLanguage"] = useLanguageCodes ? sourceLanguage : GetFullLanguageName(sourceLanguage),
            ["targetLanguage"] = useLanguageCodes ? targetLanguage : GetFullLanguageName(targetLanguage)
        };
    }
    
    /// <summary>
    /// Converts a language code to its full culture name in English.
    /// </summary>
    /// <param name="languageCode">The language code to convert (e.g., "en", "pt-BR", "zh-TW").</param>
    /// <returns>The full language name or the original code if no match is found.</returns>
    protected string GetFullLanguageName(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return languageCode;

        try
        {
            return _languageCodeService.GetCultureName(languageCode);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid language code: {LanguageCode}", languageCode);
            return languageCode;
        }
    }
}