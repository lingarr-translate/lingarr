using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class GeminiContent
{
    [JsonPropertyName("parts")] 
    public List<GeminiPart> Parts { get; set; } = [];
}
