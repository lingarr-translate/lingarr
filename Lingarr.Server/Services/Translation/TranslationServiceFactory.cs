using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;

namespace Lingarr.Server.Services.Translation;

public class TranslationServiceFactory : ITranslationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TranslationServiceFactory> _logger;

    public TranslationServiceFactory(IServiceProvider serviceProvider,
        ILogger<TranslationServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ITranslationService CreateTranslationService(string serviceType)
    {
        _logger.LogInformation($"Creating translation service with |Green|{serviceType}|/Green|");
        return serviceType.ToLower() switch
        {
            "libretranslate" => new LibreTranslateService(
                _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<LibreTranslateService>>()),

            "google" => new GoogleTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<GoogleTranslator>(),
                _serviceProvider.GetRequiredService<ILogger<GoogleTranslationService>>()
            ),

            "bing" => new BingTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<BingTranslator>(),
                _serviceProvider.GetRequiredService<ILogger<BingTranslationService>>()
            ),

            "microsoft" => new MicrosoftTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<MicrosoftTranslator>(),
                _serviceProvider.GetRequiredService<ILogger<MicrosoftTranslationService>>()
            ),

            "yandex" => new YandexTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<YandexTranslator>(),
                _serviceProvider.GetRequiredService<ILogger<YandexTranslationService>>()
            ),

            "deepl" => new DeepLTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<LibreTranslateService>>()
            ),

            "openai" => new OpenAiTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<OpenAiTranslationService>>()
            ),

            "anthropic" => new AnthropicTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<AnthropicTranslationService>>()
            ),

            "localai" => new LocalAiTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<LocalAiTranslationService>>()
            ),

            _ => throw new ArgumentException("Unsupported translation service type", nameof(serviceType))
        };
    }
}