using System.Text.Json.Serialization;

namespace Lingarr.Core.Models;

public class GitHubReleaseInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}