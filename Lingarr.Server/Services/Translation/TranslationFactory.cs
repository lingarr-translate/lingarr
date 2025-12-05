using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;

namespace Lingarr.Server.Services.Translation;

public class TranslationFactory : ITranslationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TranslationFactory> _logger;

    public TranslationFactory(IServiceProvider serviceProvider,
        ILogger<TranslationFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ITranslationService CreateTranslationService(string serviceType)
    {
        return serviceType.ToLower() switch
        {
            "libretranslate" => new LibreService(
                _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<LibreService>>()),

            "google" => new GTranslatorService<GoogleTranslator>(
                _serviceProvider.GetRequiredService<GoogleTranslator>(),
                "/app/Statics/google_languages.json",
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<GoogleTranslator>>()
            ),

            "bing" => new GTranslatorService<BingTranslator>(
                _serviceProvider.GetRequiredService<BingTranslator>(),
                "/app/Statics/bing_languages.json",
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<BingTranslator>>()
            ),

            "microsoft" => new GTranslatorService<MicrosoftTranslator>(
                _serviceProvider.GetRequiredService<MicrosoftTranslator>(),
                "/app/Statics/microsoft_languages.json",
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<MicrosoftTranslator>>()
            ),

            "yandex" => new GTranslatorService<YandexTranslator>(
                _serviceProvider.GetRequiredService<YandexTranslator>(),
                "/app/Statics/yandex_languages.json",
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<YandexTranslator>>()
            ),

            "deepl" => new DeepLService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<DeepLService>>()
            ),

            "openai" => new OpenAiService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<OpenAiService>>()
            ),

            "anthropic" => new AnthropicService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<AnthropicService>>()
            ),

            "localai" => new LocalAiService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<LocalAiService>>()
            ),

            "deepseek" => new DeepSeekService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<DeepSeekService>>()
            ),

            "chutes" => new ChutesService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<ChutesService>>()
            ),

            "gemini" => new GoogleGeminiService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<GoogleGeminiService>>()
            ),

            _ => throw new ArgumentException("Unsupported translation service type", nameof(serviceType))
        };
    }
}