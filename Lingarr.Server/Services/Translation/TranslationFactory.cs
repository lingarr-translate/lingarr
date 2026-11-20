using GTranslate.Translators;
using Lingarr.Contracts.Settings;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services.Plugins;

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
    public ITranslationService CreateTranslationService(TranslationServiceInstance instance)
    {
        var provider = instance.Provider.ToLowerInvariant();
        var settingService = CreateInstanceSettingService(instance.Id, provider);
        var languageCodeService = _serviceProvider.GetRequiredService<LanguageCodeService>();
        return provider switch
        {
            "libretranslate" => new LibreService(
                _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                settingService,
                _serviceProvider.GetRequiredService<ILogger<LibreService>>(),
                languageCodeService),

            "google" => new GTranslatorService<GoogleTranslator>(
                _serviceProvider,
                _serviceProvider.GetRequiredService<IHttpClientFactory>(),
                Path.Combine(AppContext.BaseDirectory, "Statics", "google_languages.json"),
                settingService,
                _serviceProvider.GetRequiredService<ILogger<GoogleTranslator>>(),
                languageCodeService
            ),

            "bing" => new GTranslatorService<BingTranslator>(
                _serviceProvider,
                _serviceProvider.GetRequiredService<IHttpClientFactory>(),
                Path.Combine(AppContext.BaseDirectory, "Statics", "bing_languages.json"),
                settingService,
                _serviceProvider.GetRequiredService<ILogger<BingTranslator>>(),
                languageCodeService
            ),

            "microsoft" => new GTranslatorService<MicrosoftTranslator>(
                _serviceProvider,
                _serviceProvider.GetRequiredService<IHttpClientFactory>(),
                Path.Combine(AppContext.BaseDirectory, "Statics", "microsoft_languages.json"),
                settingService,
                _serviceProvider.GetRequiredService<ILogger<MicrosoftTranslator>>(),
                languageCodeService
            ),

            "yandex" => new GTranslatorService<YandexTranslator>(
                _serviceProvider,
                _serviceProvider.GetRequiredService<IHttpClientFactory>(),
                Path.Combine(AppContext.BaseDirectory, "Statics", "yandex_languages.json"),
                settingService,
                _serviceProvider.GetRequiredService<ILogger<YandexTranslator>>(),
                languageCodeService
            ),

            "deepl" => new DeepLService(
                settingService,
                _serviceProvider.GetRequiredService<ILogger<DeepLService>>(),
                languageCodeService
            ),

            "openai" => new OpenAiService(
                settingService,
                _serviceProvider.GetRequiredService<ILogger<OpenAiService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            "anthropic" => new AnthropicService(
                settingService,
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<AnthropicService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            "localai" => new LocalAiService(
                settingService,
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<LocalAiService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            "deepseek" => new DeepSeekService(
                settingService,
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<DeepSeekService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            "mistral" => new MistralService(
                settingService,
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<MistralService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            "xai" => new XAiService(
                settingService,
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<XAiService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            "gemini" => new GoogleGeminiService(
                settingService,
                _serviceProvider.GetRequiredService<HttpClient>(),
                _serviceProvider.GetRequiredService<ILogger<GoogleGeminiService>>(),
                languageCodeService,
                _serviceProvider.GetRequiredService<IRequestTemplateService>()
            ),

            // load any registered plugins
            _ => CreatePluginService(provider, settingService)
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<TranslationServiceEntry> CreateTranslationServices(IReadOnlyList<TranslationServiceInstance> instances)
    {
        var services = new List<TranslationServiceEntry>(instances.Count);
        foreach (var instance in instances)
        {
            var provider = instance.Provider.ToLowerInvariant();
            try
            {
                var service = CreateTranslationService(instance);
                services.Add(new TranslationServiceEntry(provider, service, service as IBatchTranslationService));
            }
            catch (ArgumentException)
            {
                _logger.LogWarning(
                    "Skipping translation service {Name} with unknown provider {Provider}.",
                    instance.Name,
                    provider);
            }
        }
        return services;
    }

    private InstanceSettingService CreateInstanceSettingService(int translationServiceInstanceId, string provider)
    {
        var translationServiceInstanceService = _serviceProvider.GetRequiredService<ITranslationServiceInstanceService>();
        return new InstanceSettingService(
            _serviceProvider.GetRequiredService<ISettingService>(),
            _serviceProvider.GetRequiredService<LingarrDbContext>(),
            _serviceProvider.GetRequiredService<IEncryptionService>(),
            translationServiceInstanceId,
            translationServiceInstanceService.GetInstanceSettingKeys(provider));
    }

    private ITranslationService CreatePluginService(string provider, ISettingService settingService)
    {
        var implementationType = _serviceProvider.GetRequiredService<IPluginRegistry>()
            .Find(provider)?
            .ImplementationType;
        if (implementationType is null)
        {
            throw new ArgumentException("Unsupported translation service type", nameof(provider));
        }

        if (!AcceptsSettingsAccess(implementationType))
        {
            return (ITranslationService)ActivatorUtilities.CreateInstance(_serviceProvider, implementationType);
        }

        return (ITranslationService)ActivatorUtilities.CreateInstance(
            _serviceProvider,
            implementationType,
            new SettingsAccess(settingService));
    }

    private static bool AcceptsSettingsAccess(Type implementationType)
    {
        return implementationType
            .GetConstructors()
            .Any(constructor => constructor
                .GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(ISettingsAccess)));
    }
}