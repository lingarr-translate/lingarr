using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class AnthropicTemplate
{
    [JsonPropertyName("model")] 
    public string Model { get; set; } = "{model}";
    
    [JsonPropertyName("max_tokens")] 
    public int MaxTokens { get; set; } = 1024;
    
    [JsonPropertyName("system")] 
    public string System { get; set; } = "{systemPrompt}";

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } =
    [
        new()
        {
            Role = "user", 
            Content = "{userMessage}"
        }
    ];
}
