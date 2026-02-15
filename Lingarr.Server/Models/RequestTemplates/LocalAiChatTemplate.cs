using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class LocalAiChatTemplate
{
    [JsonPropertyName("model")] 
    public string Model { get; set; } = "{model}";

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } =
    [
        new()
        {
            Role = "system", 
            Content = "{systemPrompt}"
        },
        new() { 
            Role = "user", 
            Content = "{userMessage}" 
        }
    ];
}
