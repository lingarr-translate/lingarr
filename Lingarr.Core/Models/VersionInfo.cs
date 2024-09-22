
namespace Lingarr.Core.Models;

public class VersionInfo
{
    public bool NewVersion { get; set; }
    public string? CurrentVersion { get; set; } = string.Empty;
    public string? LatestVersion { get; set; } = string.Empty;
}