using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Models.Batch;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class RayService : BaseTranslationService, IBatchTranslationService, IDisposable
{
    private const int UploadChunkBytes = 2 * 1024 * 1024;
    private const int MaxCuesPerJob = 500;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private string? _apiUrl;
    private string? _apiKey;
    private string _target = "local";
    private int _cuesPerJob = 200;
    private int _timeoutMinutes = 180;
    private bool _initialized;
    private bool _disposed;

    /// <inheritdoc />
    public override string? ModelName => "Sunlight (Ray Local)";

    public RayService(
        ISettingService settings,
        ILogger<RayService> logger,
        LanguageCodeService languageCodeService)
        : base(settings, logger, languageCodeService)
    {
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken)
    {
        var translated = await TranslateBatchAsync(
            [new BatchSubtitleItem { Position = 0, Line = text }],
            sourceLanguage,
            targetLanguage,
            cancellationToken);
        return translated[0];
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, string>> TranslateBatchAsync(
        List<BatchSubtitleItem> subtitleBatch,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);
        var translations = new Dictionary<int, string>();

        foreach (var batch in subtitleBatch.Chunk(_cuesPerJob))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await TranslateSrtBatchAsync(
                batch,
                targetLanguage,
                cancellationToken);

            if (result.Count != batch.Length)
            {
                throw new TranslationException(
                    $"Ray returned {result.Count} subtitle cues for a batch of {batch.Length}.");
            }

            for (var index = 0; index < batch.Length; index++)
            {
                translations[batch[index].Position] = result[index];
            }
        }

        return translations;
    }

    /// <inheritdoc />
    public override Task<List<SourceLanguage>> GetLanguages() =>
        Task.FromResult(new List<SourceLanguage>());

    /// <inheritdoc />
    public override Task<ModelsResponse> GetModels() => Task.FromResult(new ModelsResponse
    {
        Options = [new LabelValue { Label = "Sunlight", Value = "best" }]
    });

    /// <inheritdoc />
    public override Task<LanguagePair?> GetLanguagePair(
        string requestedSource,
        string requestedTarget,
        CancellationToken cancellationToken) =>
        Task.FromResult<LanguagePair?>(new LanguagePair
        {
            Source = requestedSource,
            Target = requestedTarget,
            Tier = MatchTier.Exact
        });

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _initLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.Ray.Endpoint,
                SettingKeys.Translation.Ray.Target,
                SettingKeys.Translation.Ray.BatchSize,
                SettingKeys.Translation.Ray.JobTimeoutMinutes,
                SettingKeys.Translation.RequestTimeout
            ]);

            _apiUrl = settings[SettingKeys.Translation.Ray.Endpoint]?.Trim().TrimEnd('/');
            _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.Ray.ApiKey);
            if (string.IsNullOrWhiteSpace(_apiUrl) || string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException(
                    "Configure the Ray server URL and API key in Settings → Services.");
            }

            if (!Uri.TryCreate(_apiUrl, UriKind.Absolute, out var baseAddress)
                || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("The Ray endpoint must be an absolute HTTP or HTTPS URL.");
            }

            _target = settings[SettingKeys.Translation.Ray.Target]?.Trim().ToLowerInvariant() ?? "local";
            if (_target is not ("local" or "cloud"))
            {
                throw new InvalidOperationException("Ray processing target must be 'local' or 'cloud'.");
            }

            _cuesPerJob = ParseBoundedSetting(
                settings[SettingKeys.Translation.Ray.BatchSize], 200, 1, MaxCuesPerJob);
            _timeoutMinutes = ParseBoundedSetting(
                settings[SettingKeys.Translation.Ray.JobTimeoutMinutes], 180, 1, 360);

            var requestTimeout = int.TryParse(settings[SettingKeys.Translation.RequestTimeout], out var timeOut)
                ? timeOut
                : 5;
            // Ray jobs can run longer than the shared request timeout; keep a floor of the job timeout.
            _httpClient.Timeout = TimeSpan.FromMinutes(Math.Max(requestTimeout, _timeoutMinutes));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<List<string>> TranslateSrtBatchAsync(
        BatchSubtitleItem[] batch,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var fileName = $"lingarr-ray-{Guid.NewGuid():N}.srt";
        var srtBytes = Encoding.UTF8.GetBytes(CreateSrt(batch));
        var uploadId = await UploadAsync(fileName, srtBytes, cancellationToken);

        try
        {
            var jobId = await CreateJobAsync(fileName, uploadId, targetLanguage, cancellationToken);
            await WaitForCompletionAsync(jobId, cancellationToken);
            var translatedSrt = await DownloadResultAsync(jobId, cancellationToken);
            return ParseSrt(Encoding.UTF8.GetString(translatedSrt));
        }
        finally
        {
            await DeleteUploadAsync(uploadId, cancellationToken);
        }
    }

    private async Task<string> UploadAsync(
        string fileName,
        byte[] contents,
        CancellationToken cancellationToken)
    {
        using var createResponse = await SendJsonAsync(
            HttpMethod.Post,
            "/v1/uploads",
            new { file_name = fileName, size = contents.Length },
            cancellationToken);
        var uploadId = createResponse.RootElement.GetProperty("upload_id").GetString();
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new TranslationException("Ray did not return an upload ID.");
        }

        var offset = createResponse.RootElement.TryGetProperty("next_offset", out var initialOffset)
            ? initialOffset.GetInt32()
            : 0;

        while (offset < contents.Length)
        {
            var count = Math.Min(UploadChunkBytes, contents.Length - offset);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_apiUrl}/v1/uploads/{Uri.EscapeDataString(uploadId)}/chunks?offset={offset}");
            request.Content = new ByteArrayContent(contents, offset, count);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var chunk = contents.AsSpan(offset, count);
            request.Headers.TryAddWithoutValidation(
                "X-Ray-Chunk-Sha256",
                Convert.ToHexString(SHA256.HashData(chunk)).ToLowerInvariant());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            using var chunkResponse = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            offset = chunkResponse.RootElement.GetProperty("next_offset").GetInt32();
        }

        using var completeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_apiUrl}/v1/uploads/{Uri.EscapeDataString(uploadId)}/complete");
        using var completeResponse = await _httpClient.SendAsync(completeRequest, cancellationToken);
        await EnsureSuccessAsync(completeResponse, cancellationToken);
        return uploadId;
    }

    private async Task<string> CreateJobAsync(
        string fileName,
        string uploadId,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var request = new Dictionary<string, object>
        {
            ["quality"] = "best",
            ["format"] = "srt",
            ["coverage"] = "standard",
            ["target"] = _target,
            ["subtitle_source"] = "translate",
            ["target_language"] = targetLanguage,
            ["upload_id"] = uploadId,
            ["file_name"] = fileName
        };

        using var response = await SendJsonAsync(
            HttpMethod.Post,
            "/v1/subtitles",
            request,
            cancellationToken);
        var jobId = response.RootElement.GetProperty("job_id").GetString();
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new TranslationException("Ray did not return a job ID.");
        }

        _logger.LogInformation("Submitted subtitle batch {FileName} to Ray as job {JobId}.",
            fileName, jobId);
        return jobId;
    }

    private async Task WaitForCompletionAsync(string jobId, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(_timeoutMinutes));

        while (true)
        {
            using var jobsResponse = await SendJsonAsync(
                HttpMethod.Get,
                "/v1/jobs",
                null,
                timeout.Token);
            var jobs = jobsResponse.RootElement.GetProperty("jobs");
            var job = jobs.EnumerateArray().FirstOrDefault(candidate =>
                candidate.TryGetProperty("job_id", out var id)
                && id.GetString() == jobId);

            if (job.ValueKind == JsonValueKind.Undefined)
            {
                throw new TranslationException($"Ray job {jobId} disappeared before completing.");
            }

            var status = job.GetProperty("status").GetString();
            if (status == "completed")
            {
                return;
            }

            if (status is "failed" or "canceled")
            {
                var error = job.TryGetProperty("error", out var errorValue)
                    ? errorValue.GetString()
                    : null;
                throw new TranslationException($"Ray job {jobId} {status}: {error ?? "no details"}");
            }

            await Task.Delay(PollInterval, timeout.Token);
        }
    }

    private async Task<byte[]> DownloadResultAsync(string jobId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{_apiUrl}/v1/jobs/{Uri.EscapeDataString(jobId)}/result",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task DeleteUploadAsync(string uploadId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync(
                $"{_apiUrl}/v1/uploads/{Uri.EscapeDataString(uploadId)}",
                cancellationToken);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Ray upload cleanup returned HTTP {StatusCode}.", (int)response.StatusCode);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(exception, "Could not clean up the temporary Ray upload.");
        }
    }

    private async Task<JsonDocument> SendJsonAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{_apiUrl}{path}");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > 1000)
        {
            body = body[..1000];
        }

        throw new TranslationException(
            $"Ray API returned HTTP {(int)response.StatusCode}: {body}");
    }

    private static string CreateSrt(BatchSubtitleItem[] batch)
    {
        var output = new StringBuilder();
        for (var index = 0; index < batch.Length; index++)
        {
            var start = TimeSpan.FromSeconds(index * 5).ToString(@"hh\:mm\:ss\,fff");
            var end = TimeSpan.FromSeconds(index * 5 + 4).ToString(@"hh\:mm\:ss\,fff");
            var text = (batch[index].Line ?? string.Empty)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Trim();

            output.AppendLine((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            output.Append(start).Append(" --> ").AppendLine(end);
            output.AppendLine(text);
            output.AppendLine();
        }

        return output.ToString();
    }

    private static List<string> ParseSrt(string content)
    {
        var normalized = content.TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal);
        var blocks = Regex.Split(normalized.Trim(), @"\n\s*\n+");
        var cues = new List<string>(blocks.Length);
        foreach (var block in blocks)
        {
            var lines = block.Split('\n');
            var timingLine = Array.FindIndex(lines, line => line.Contains("-->", StringComparison.Ordinal));
            if (timingLine >= 0 && timingLine + 1 < lines.Length)
            {
                cues.Add(string.Join(" ", lines[(timingLine + 1)..]).Trim());
            }
        }

        return cues;
    }

    private static int ParseBoundedSetting(string? value, int fallback, int minimum, int maximum) =>
        int.TryParse(value, out var parsed) ? Math.Clamp(parsed, minimum, maximum) : fallback;
}
