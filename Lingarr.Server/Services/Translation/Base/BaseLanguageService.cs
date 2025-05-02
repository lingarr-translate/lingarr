using System.Globalization;
using System.Text.Json;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

public abstract class BaseLanguageService : BaseTranslationService
{
    private readonly string _languageFilePath;
    protected string? _contextPrompt;
    protected string? _contextPromptEnabled;
    protected Dictionary<string, string> _replacements;
    protected List<KeyValuePair<string, object>>? _customParameters;

    protected BaseLanguageService(
        ISettingService settings,
        ILogger logger,
        string languageFilePath) : base(settings, logger)
    {
        _languageFilePath = languageFilePath;
    }
    
    /// <summary>
    /// Prepares custom parameters from settings for use in API requests.
    /// </summary>
    /// <param name="settings">Dictionary containing application settings.</param>
    /// <param name="parameterKey">The key to access the custom parameters in the settings.</param>
    protected List<KeyValuePair<string, object>>? PrepareCustomParameters(Dictionary<string, string> settings, string parameterKey)
    {
        if (!settings.TryGetValue(parameterKey, out var parametersJson) ||
            string.IsNullOrEmpty(parametersJson))
        {
            return null;
        }

        try
        {
            var parametersArray = JsonSerializer.Deserialize<JsonElement[]>(parametersJson);
            if (parametersArray == null)
            {
                return null;
            }

            var parameters = new List<KeyValuePair<string, object>>();
            foreach (var param in parametersArray)
            {
                if (!param.TryGetProperty("key", out var key) ||
                    !param.TryGetProperty("value", out var value)) continue;
    
                object valueObj = value.ValueKind switch
                {
                    JsonValueKind.String => TryParseNumeric(value.GetString()!),
                    JsonValueKind.Number => value.TryGetInt64(out var intVal) ? intVal : value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => value.GetString()!
                };

                parameters.Add(new KeyValuePair<string, object>(key.GetString()!, valueObj));
            }
            return parameters;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse custom parameters: {Parameters}", parametersJson);
            return null;
        }
    }
    
    private static object TryParseNumeric(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
        {
            return floatVal; 
        }
    
        return value;
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

    
    /// <summary>
    /// Adds custom parameters to the request data if they exist.
    /// </summary>
    /// <param name="requestData">The dictionary containing the base request parameters.</param>
    protected Dictionary<string, object> AddCustomParameters(Dictionary<string, object> requestData)
    {
        if (_customParameters != null && _customParameters.Count > 0)
        {
            foreach (var param in _customParameters)
            {
                requestData[param.Key] = param.Value;
            }
        }
    
        return requestData;
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