using GTranslate.Translators;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;

namespace Lingarr.Server.Services.Translation;

public class TranslationServiceFactory : ITranslationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TranslationServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public ITranslationService CreateTranslationService(string serviceType)
    {
        return serviceType.ToLower() switch
        {
            "libretranslate" => new LibreTranslateService(
                _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<LibreTranslateService>>()),

            "deepl" => new DeepLTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<LibreTranslateService>>()
            ),

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

            "openai" => new OpenAiTranslationService(
                _serviceProvider.GetRequiredService<ISettingService>(),
                _serviceProvider.GetRequiredService<ILogger<OpenAiTranslationService>>()
            ),

            _ => throw new ArgumentException("Unsupported translation service type", nameof(serviceType))
        };
    }
}