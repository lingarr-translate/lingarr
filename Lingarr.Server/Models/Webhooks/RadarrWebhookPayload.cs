using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Webhooks;

public class RadarrWebhookPayload
{
    [JsonPropertyName("movie")]
    public RadarrWebhookMovie? Movie { get; set; }
}

public class RadarrWebhookMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
