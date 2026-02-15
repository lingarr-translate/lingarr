using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class OllamaGenerateTemplate
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "{model}";
    
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "{systemPrompt}\n\n{userMessage}";
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}
