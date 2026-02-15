using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class GeminiPart
{
    [JsonPropertyName("text")] 
    public string Text { get; set; } = string.Empty;
}
