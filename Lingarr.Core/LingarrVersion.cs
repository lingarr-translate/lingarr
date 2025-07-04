using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Lingarr.Core.Models;

namespace Lingarr.Core;

public static class LingarrVersion
{
    public const string Number = "1.0.0";

    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "LingarrApp" } }
    };
    private const string GitHubApiUrl = "https://api.github.com/repos/lingarr-translate/lingarr/releases/latest";
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

    public static async Task<VersionInfo> CheckForUpdates()
    {
        var latestVersion = await GetLatestVersion();

        return new VersionInfo
        {
            NewVersion = IsNewVersionAvailable(latestVersion, Number),
            CurrentVersion = Number,
            LatestVersion = latestVersion
        };
    }

    private static async Task<string> GetLatestVersion()
    {
        var cacheKey = "GithubLatestRelease";
        if (Cache.TryGetValue(cacheKey, out string? cachedVersion) && cachedVersion != null)
        {
            return cachedVersion;
        }

        try
        {
            var release = await HttpClient.GetFromJsonAsync<GitHubReleaseInfo>(GitHubApiUrl);
            var latestVersion = release?.Name ?? Number;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            Cache.Set(cacheKey, latestVersion, cacheEntryOptions);

            return latestVersion;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get latest version, returning default application version. Error: {ex.Message}");
            return Number;
        }
    }

    private static bool IsNewVersionAvailable(string latestVersion, string currentVersion)
        => Version.Parse(latestVersion.TrimStart('v')) > Version.Parse(currentVersion);
}