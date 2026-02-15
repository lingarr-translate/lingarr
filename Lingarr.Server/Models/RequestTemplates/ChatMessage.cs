using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class ChatMessage
{
    [JsonPropertyName("role")] 
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")] 
    public string Content { get; set; } = string.Empty;
}
