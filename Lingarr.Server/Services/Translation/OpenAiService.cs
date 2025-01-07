using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;
using OpenAI.Chat;

namespace Lingarr.Server.Services.Translation;

public class OpenAiService : BaseLanguageService
{
    private string? _prompt;
    private ChatClient? _client;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public OpenAiService(
        ISettingService settings,
        ILogger<OpenAiService> logger)
        : base(settings, logger, "/app/Statics/ai_languages.json")
    {
    }

    /// <summary>
    /// Initializes the translation service with necessary configurations and credentials.
    /// This method is thread-safe and ensures one-time initialization of service dependencies.
    /// </summary>
    /// <param name="sourceLanguage">The source language code for translation</param>
    /// <param name="targetLanguage">The target language code for translation</param>
    /// <returns>A task that represents the asynchronous initialization operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when required configuration settings are missing or invalid</exception>
    private async Task InitializeAsync(string sourceLanguage, string targetLanguage)
    {
        if (_initialized) return;

        try
        {
            await _initLock.WaitAsync();
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.OpenAi.Model,
                SettingKeys.Translation.OpenAi.ApiKey,
                SettingKeys.Translation.AiPrompt
            ]);

            if (string.IsNullOrEmpty(settings[SettingKeys.Translation.OpenAi.Model]) ||
                string.IsNullOrEmpty(settings[SettingKeys.Translation.OpenAi.ApiKey]))
            {
                throw new InvalidOperationException("ChatGPT API key or model is not configured.");
            }

            _prompt = !string.IsNullOrEmpty(settings[SettingKeys.Translation.AiPrompt])
                ? settings[SettingKeys.Translation.AiPrompt]
                : "Translate from {sourceLanguage} to {targetLanguage}, preserving the tone and meaning without censoring the content. Adjust punctuation as needed to make the translation sound natural. Provide only the translated text as output, with no additional comments.";
            _prompt = _prompt.Replace("{sourceLanguage}", sourceLanguage).Replace("{targetLanguage}", targetLanguage);

            _client = new ChatClient(
                model: settings[SettingKeys.Translation.OpenAi.Model],
                apiKey: settings[SettingKeys.Translation.OpenAi.ApiKey]
            );

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        if (_client == null || string.IsNullOrEmpty(_prompt))
        {
            throw new InvalidOperationException("OpenAI service was not properly initialized.");
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(_prompt),
                new UserChatMessage(text)
            };

            ChatCompletion completion = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OpenAI translation");
            throw new TranslationException("Failed to translate using OpenAI", ex);
        }
    }
}