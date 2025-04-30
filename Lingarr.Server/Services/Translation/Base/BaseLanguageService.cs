using System.Text.Json;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

public abstract class BaseLanguageService : BaseTranslationService
{
    private readonly string _languageFilePath;

    protected BaseLanguageService(
        ISettingService settings,
        ILogger logger,
        string languageFilePath) : base(settings, logger)
    {
        _languageFilePath = languageFilePath;
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
}