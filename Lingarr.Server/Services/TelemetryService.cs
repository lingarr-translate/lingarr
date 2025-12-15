using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lingarr.Core;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Telemetry;

namespace Lingarr.Server.Services;

public class TelemetryService : ITelemetryService
{
    private readonly IStatisticsService _statisticsService;
    private readonly ISettingService _settingService;
    private readonly ILogger<TelemetryService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;

    public TelemetryService(
        IStatisticsService statisticsService,
        ISettingService settingService,
        ILogger<TelemetryService> logger,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment)
    {
        _statisticsService = statisticsService;
        _settingService = settingService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
    }

    public async Task<TelemetryPayload> GenerateTelemetryPayload()
    {
        var stats = await _statisticsService.GetStatistics();
        var deltaLines = Math.Max(0, stats.TotalLinesTranslated - await GetNumericSetting(SettingKeys.Telemetry.LastReportedLines));
        var deltaFiles = Math.Max(0, stats.TotalFilesTranslated - await GetNumericSetting(SettingKeys.Telemetry.LastReportedFiles));
        var deltaChars = Math.Max(0, stats.TotalCharactersTranslated - await GetNumericSetting(SettingKeys.Telemetry.LastReportedCharacters));

        return new TelemetryPayload
        {
            Version = LingarrVersion.Number,
            ReportDate = $"{DateTime.UtcNow:yyyy-MM-dd}",
            Platform = GetPlatformInfo(),
            Metrics = new TelemetryMetrics
            {
                FilesTranslated = deltaFiles,
                LinesTranslated = deltaLines,
                CharactersTranslated = deltaChars,
                ServiceUsage = stats.TranslationsByService,
                LanguagePairs = stats.SubtitlesByLanguage,
                MediaTypeUsage = stats.TranslationsByMediaType,
                ModelUsage = stats.TranslationsByModel
            }
        };
    }

    private async Task<long> GetNumericSetting(string key)
    {
        var value = await _settingService.GetSetting(key);
        return long.TryParse(value, out var result) ? result : 0;
    }

    public async Task<bool> CanSubmitTelemetry()
    {
        if (_environment.IsDevelopment())
        {
            return false;
        }

        if (await _settingService.GetSetting(SettingKeys.Telemetry.Enabled) != "true")
        {
            return false;
        }

        var lastSubmission = await _settingService.GetSetting(SettingKeys.Telemetry.LastSubmission);
        if (string.IsNullOrEmpty(lastSubmission) || !DateTime.TryParse(lastSubmission, out var lastDate))
        {
            return true;
        }

        return DateTime.UtcNow - lastDate >= TimeSpan.FromDays(7);
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
            var request = new HttpRequestMessage(HttpMethod.Post,
                Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9hcGkubGluZ2Fyci5jb20v")))
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

            // Update snapshot on success
            var stats = await _statisticsService.GetStatistics();
            await _settingService.SetSetting(SettingKeys.Telemetry.LastSubmission, DateTime.UtcNow.ToString("O"));
            await _settingService.SetSetting(SettingKeys.Telemetry.LastReportedLines, stats.TotalLinesTranslated.ToString());
            await _settingService.SetSetting(SettingKeys.Telemetry.LastReportedFiles, stats.TotalFilesTranslated.ToString());
            await _settingService.SetSetting(SettingKeys.Telemetry.LastReportedCharacters, stats.TotalCharactersTranslated.ToString());

            _logger.LogInformation("Telemetry submitted successfully with HMAC authentication");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting telemetry");
            return false;
        }
    }

    private string GenerateHmac(string payload)
    {
        using var hmac = new HMACSHA256("tSBTCU4Qv76so0c2U8bBX0faSzc3uc6Z"u8.ToArray());
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }


    private string GetPlatformInfo()
    {
        var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"linux/{arch}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"windows/{arch}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"macos/{arch}";
        }
        return $"unknown/{arch}";
    }
}
