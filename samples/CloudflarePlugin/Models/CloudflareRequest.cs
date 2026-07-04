using System.Text.Json.Serialization;

namespace Lingarr.Plugin.Cloudflare.Models;

public class CloudflareRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("source_lang")]
    public string SourceLang { get; set; } = string.Empty;

    [JsonPropertyName("target_lang")]
    public string TargetLang { get; set; } = string.Empty;
}
