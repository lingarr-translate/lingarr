using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lingarr.Core;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Telemetry;
using Microsoft.Extensions.Caching.Memory;

namespace Lingarr.Server.Services;

public class LingarrApiService : ILingarrApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LingarrApiService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;
    private const string CacheKeyLatestVersion = "LingarrApi_LatestVersion";

    public LingarrApiService(
        IHttpClientFactory httpClientFactory,
        ILogger<LingarrApiService> logger,
        IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;

        _baseUrl = new UriBuilder
        {
            Scheme = Uri.UriSchemeHttps,
            Host = $"api.{LingarrVersion.Name.ToLower()}.com"
        }.Uri.ToString();
    }

    public async Task<string?> GetLatestVersion()
    {
        // Check cache first
        if (_cache.TryGetValue(CacheKeyLatestVersion, out string? cachedVersion))
        {
            _logger.LogDebug("Returning cached version information from Lingarr API");
            return cachedVersion;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"{LingarrVersion.Name}/{LingarrVersion.Number}");

            var response = await httpClient.GetAsync($"{_baseUrl}/version/latest");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get latest version from Lingarr API: {StatusCode}",
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var versionResponse = JsonSerializer.Deserialize<VersionResponse>(content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (versionResponse?.Version != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(24));
                _cache.Set(CacheKeyLatestVersion, versionResponse.Version, cacheOptions);

                _logger.LogInformation("Retrieved latest version: {Version}", versionResponse.Version);
                return versionResponse.Version;
            }

            _logger.LogWarning("Lingarr API returned empty version");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest version");
            return null;
        }
    }

    public async Task<bool> SubmitTelemetry(TelemetryPayload payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var signature = GenerateHmac(json);
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Signature", signature);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telemetry submission failed: {Status} - {Response}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync());
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private string GenerateHmac(string payload)
    {
        using var hmac = new HMACSHA256("tSBTCU4Qv76so0c2U8bBX0faSzc3uc6Z"u8.ToArray());
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private class VersionResponse
    {
        public string? Version { get; set; }
    }
}
