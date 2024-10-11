using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using OpenAI.Chat;

namespace Lingarr.Server.Services.Translation;

public class OpenAiTranslationService : TranslationServiceBase
{
    public OpenAiTranslationService(
        ISettingService settings,
        ILogger<OpenAiTranslationService> logger) : base(settings, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var openAi = await _settings.GetSettings(["openai_model", "openai_api_key"]);
        if (string.IsNullOrEmpty(openAi["openai_model"]) || string.IsNullOrEmpty(openAi["openai_api_key"]))
        {
            throw new InvalidOperationException("ChatGPT API key or model is not configured.");
        }
        var prompt = $"Translate the following text from {sourceLanguage} to {targetLanguage}. Only provide the translation, without any additional comments or explanations:\n\n{text}";

        try
        {
            ChatClient client = new ChatClient(model: openAi["openai_model"], apiKey: openAi["openai_api_key"]);
            ChatCompletion completion = client.CompleteChat(prompt);

            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OpenAI translation");
            throw new TranslationException("Failed to translate using OpenAI", ex);
        }
    }
}