using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class GenerateResponse
{
    [JsonPropertyName("model")] 
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("response")] 
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")] 
    public bool Done { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("choices")] 
    public List<ChatChoice> Choices { get; set; } = new();
}

public class ChatChoice
{
    [JsonPropertyName("message")] 
    public ChatMessage Message { get; set; } = new();
}

public class ChatMessage
{
    [JsonPropertyName("content")] 
    public string Content { get; set; } = string.Empty;
}