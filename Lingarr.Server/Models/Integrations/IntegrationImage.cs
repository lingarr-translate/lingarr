using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class IntegrationImage
{
    [JsonPropertyName("coverType")]
    public string? CoverType { get; set; }
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}