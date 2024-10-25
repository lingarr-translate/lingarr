using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class IntegrationImage
{
    [JsonPropertyName("coverType")]
    public required string CoverType { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}