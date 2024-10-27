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
        var settings = await _settings.GetSettings(["openai_model", "openai_api_key", "ai_prompt"]);
        if (string.IsNullOrEmpty(settings["openai_model"]) || string.IsNullOrEmpty(settings["openai_api_key"]))
        {
            throw new InvalidOperationException("ChatGPT API key or model is not configured.");
        }
        
        var prompt = !string.IsNullOrEmpty(settings["ai_prompt"])
            ? settings["ai_prompt"] 
            : $"Translate from {sourceLanguage} to {targetLanguage}, preserving the tone and meaning without censoring the content. Adjust punctuation as needed to make the translation sound natural. Provide only the translated text as output, with no additional comments.";
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(prompt),
                new UserChatMessage(text)
            };
            ChatClient client = new ChatClient(model: settings["openai_model"], apiKey: settings["openai_api_key"]);
            ChatCompletion completion = client.CompleteChat(messages);

            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OpenAI translation");
            throw new TranslationException("Failed to translate using OpenAI", ex);
        }
    }
}