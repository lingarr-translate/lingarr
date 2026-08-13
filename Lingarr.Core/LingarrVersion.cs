using System.Reflection;
using System.Runtime.InteropServices;
using Lingarr.Core.Models;

namespace Lingarr.Core;

public static class LingarrVersion
{
    public const string Name = "Lingarr";

    public static readonly string Number = GetCurrentVersion();

    public static string Platform { get; } = BuildPlatform();

    private static string BuildPlatform()
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

    private static string GetCurrentVersion()
    {
        var attribute = typeof(LingarrVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = attribute?.InformationalVersion ?? "0.0.0";

        var buildMetadataStart = version.IndexOf('+');
        return buildMetadataStart < 0 ? version : version[..buildMetadataStart];
    }

    public static async Task<VersionInfo> CheckForUpdates(object? lingarrApiService = null)
    {
        var latestVersion = Number;
        var isDevelopment = !Version.TryParse(Number, out var current);

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
        catch (Exception)
        {
            Console.WriteLine("Failed to get latest version");
        }

        var newVersion = !isDevelopment
            && Version.TryParse(latestVersion.TrimStart('v'), out var latest)
            && latest > current;

        return new VersionInfo
        {
            NewVersion = newVersion,
            IsDevelopment = isDevelopment,
            CurrentVersion = Number,
            LatestVersion = latestVersion
        };
    }
}
