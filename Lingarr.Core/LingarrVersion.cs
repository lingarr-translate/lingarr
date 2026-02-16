using Lingarr.Core.Models;

namespace Lingarr.Core;

public static class LingarrVersion
{
    public const string Name = "Lingarr";
    public const string Number = "1.0.7";

    public static async Task<VersionInfo> CheckForUpdates(object? lingarrApiService = null)
    {
        var latestVersion = Number;
        
        try
        {
            var method = lingarrApiService?.GetType().GetMethod("GetLatestVersion");
            if (method != null)
            {
                if (method.Invoke(lingarrApiService, null) is Task<string?> task)
                {
                    var version = await task;
                    if (!string.IsNullOrEmpty(version))
                    {
                        latestVersion = version;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get latest version");
        }

        return new VersionInfo
        {
            NewVersion = IsNewVersionAvailable(latestVersion, Number),
            CurrentVersion = Number,
            LatestVersion = latestVersion
        };
    }

    private static bool IsNewVersionAvailable(string latestVersion, string currentVersion)
        => Version.Parse(latestVersion.TrimStart('v')) > Version.Parse(currentVersion);
}
