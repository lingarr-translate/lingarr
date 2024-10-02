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
            
            _ => throw new ArgumentException("Unsupported translation service type", nameof(serviceType))
        };
    }
}