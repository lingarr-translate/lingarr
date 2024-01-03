using System.Text.Json;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services;

public class LanguageService
{
    /// <summary>
    /// Retrieves the list of languages from a file.
    /// </summary>
    /// <returns>A task that resolves to a list of language objects.</returns>
    /// <exception cref="Exception">If there is an error reading or parsing the file.</exception>
    public async Task<List<LanguageModel>> GetLanguagesAsync()
    {
        try
        {
            string languageConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "Statics/config", "languages.json");
            string languagePath = Path.Combine(Directory.GetCurrentDirectory(), "Statics/replace", "languages.json");

            // If no custom config for languages is provided, use the default.
            string fileContent = File.Exists(languageConfigPath)
                ? await File.ReadAllTextAsync(languageConfigPath)
                : await File.ReadAllTextAsync(languagePath);

            return JsonSerializer.Deserialize<List<LanguageModel>>(fileContent) ?? new List<LanguageModel>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return new List<LanguageModel>();
        }
    }
}

