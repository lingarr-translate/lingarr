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
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage($"You will be provided with a sentence in {sourceLanguage} and your task is to translate it into {targetLanguage}"),
                new UserChatMessage(text)
            };
            ChatClient client = new ChatClient(model: openAi["openai_model"], apiKey: openAi["openai_api_key"]);
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