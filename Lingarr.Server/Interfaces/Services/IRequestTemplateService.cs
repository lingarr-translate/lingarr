namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for managing AI provider request templates.
/// Provides default JSON templates for each provider and handles placeholder substitution.
/// </summary>
public interface IRequestTemplateService
{
    /// <summary>
    /// Retrieves the default request templates for all AI providers.
    /// </summary>
    /// <returns>A dictionary mapping setting keys to their default JSON template strings.</returns>
    Dictionary<string, string> GetDefaultTemplates();

    /// <summary>
    /// Retrieves the default request template for a specific AI provider.
    /// </summary>
    /// <param name="settingKey">The setting key identifying the provider template.</param>
    /// <returns>The default JSON template string, or <c>null</c> if the key is not recognized.</returns>
    string? GetDefaultTemplate(string settingKey);

    /// <summary>
    /// Replaces placeholders in a JSON template string with JSON-escaped values.
    /// </summary>
    /// <param name="template">A JSON template string containing placeholders such as {model}, {systemPrompt}, and {userMessage}.</param>
    /// <param name="placeholders">A dictionary mapping placeholder names to their replacement values.</param>
    /// <returns>A valid JSON string with all placeholders replaced.</returns>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the resulting string is not valid JSON.</exception>
    string BuildRequestBody(string template, Dictionary<string, string> placeholders);
}
