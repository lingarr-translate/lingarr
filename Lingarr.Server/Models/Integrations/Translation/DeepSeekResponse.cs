using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations.Translation;

public class DeepSeekChatResponse
{
    [JsonPropertyName("choices")] 
    public List<DeepSeekChoice> Choices { get; set; } = new();
}

public class DeepSeekChoice
{
    [JsonPropertyName("message")] 
    public DeepSeekMessage Message { get; set; } = new();
}

public class DeepSeekMessage
{
    [JsonPropertyName("content")] 
    public string Content { get; set; } = string.Empty;
}