using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Plugins;
using Lingarr.Contracts.Settings;
using Lingarr.Contracts.Translation;
using Lingarr.Plugin.Cloudflare.Models;
using Microsoft.Extensions.Logging;

namespace Lingarr.Plugin.Cloudflare;

[PluginProvider("cloudflare")]
public sealed class CloudflareTranslator : ITranslationService, IDisposable
{
    private const string DefaultModel = "@cf/meta/m2m100-1.2b";

    private readonly ISettingsAccess _settings;
    private readonly ILogger<CloudflareTranslator> _logger;
    private readonly HttpClient _httpClient = new();

    private string? _accountId;
    private string? _apiToken;
    private string? _model;
    private TranslationHttpSettings? _httpSettings;

    public CloudflareTranslator(ISettingsAccess settings, ILogger<CloudflareTranslator> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public string? ModelName => _model;

    public Task<List<SourceLanguage>> GetLanguages() => Task.FromResult(new List<SourceLanguage>());

    private async Task InitializeAsync()
    {
        if (_httpSettings is not null)
        {
            return;
        }

        _accountId = await _settings.GetSettingAsync("cloudflare_account_id");
        _apiToken = await _settings.GetEncryptedSettingAsync("cloudflare_api_token");

        if (string.IsNullOrWhiteSpace(_accountId) || string.IsNullOrWhiteSpace(_apiToken))
        {
            throw new InvalidOperationException(
                "Cloudflare Workers AI plugin requires both an account ID and an API token.");
        }

        var configuredModel = await _settings.GetSettingAsync("cloudflare_model");
        _model = !string.IsNullOrWhiteSpace(configuredModel) ? configuredModel : DefaultModel;

        var httpSettings = await _settings.GetHttpSettingsAsync();
        _httpClient.Timeout = httpSettings.Timeout;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpSettings = httpSettings;
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken)
    {
        await InitializeAsync();

        var httpSettings = _httpSettings!;
        var delay = httpSettings.RetryDelay;

        for (var attempt = 1; attempt <= httpSettings.MaxRetries; attempt++)
        {
            try
            {
                return await TranslateWithCloudflare(text, sourceLanguage, targetLanguage, cancellationToken);
            }
            catch (HttpRequestException exception)
                when (exception.StatusCode is HttpStatusCode.TooManyRequests
                                              or HttpStatusCode.ServiceUnavailable)
            {
                if (attempt == httpSettings.MaxRetries)
                {
                    _logger.LogError(
                        exception,
                        "Cloudflare Workers AI retries exhausted ({StatusCode}).",
                        exception.StatusCode);
                    throw new TranslationException(
                        $"Retry limit reached after {exception.StatusCode}.",
                        exception);
                }

                _logger.LogWarning(
                    "Cloudflare Workers AI received {StatusCode}. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    exception.StatusCode,
                    delay,
                    attempt,
                    httpSettings.MaxRetries);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromTicks(delay.Ticks * httpSettings.RetryDelayMultiplier);
            }
        }

        throw new TranslationException("Cloudflare Workers AI failed after maximum retry attempts.");
    }

    private async Task<string> TranslateWithCloudflare(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{_model}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiToken!) },
            Content = JsonContent.Create(new CloudflareRequest
            {
                Text = text,
                SourceLang = sourceLanguage,
                TargetLang = targetLanguage
            })
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.ServiceUnavailable)
            {
                throw new HttpRequestException(
                    $"Cloudflare Workers AI returned {response.StatusCode}",
                    null,
                    response.StatusCode);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TranslationException(
                $"Cloudflare Workers AI request failed with status {response.StatusCode}: {body}");
        }

        var cloudflareResponse = await response.Content.ReadFromJsonAsync<CloudflareResponse>(cancellationToken);
        if (cloudflareResponse is null || !cloudflareResponse.Success)
        {
            var firstApiError = cloudflareResponse?.Errors.FirstOrDefault();
            var errorDetail = firstApiError is not null
                ? $"{firstApiError.Code}: {firstApiError.Message}"
                : "unknown error";
            throw new TranslationException($"Cloudflare Workers AI request failed ({errorDetail}).");
        }

        var translatedText = cloudflareResponse.Result?.TranslatedText;
        if (string.IsNullOrEmpty(translatedText))
        {
            throw new TranslationException("Cloudflare Workers AI returned an empty translation.");
        }

        return translatedText;
    }

    public Task<ModelsResponse> GetModels()
    {
        return Task.FromResult(new ModelsResponse
        {
            Options =
            [
                new LabelValue { Label = "m2m100 (1.2B)", Value = "@cf/meta/m2m100-1.2b" },
                new LabelValue { Label = "NLLB-200 (distilled 600M)", Value = "@cf/meta/nllb-200" }
            ]
        });
    }

    public Task<LanguagePair?> GetLanguagePair(
        string requestedSource,
        string requestedTarget,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<LanguagePair?>(new LanguagePair
        {
            Source = requestedSource,
            Target = requestedTarget,
            Tier = MatchTier.Exact
        });
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
