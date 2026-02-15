using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

public class GeminiTemplate
{
    [JsonPropertyName("systemInstruction")]
    public GeminiContent SystemInstruction { get; set; } = new()
    {
        Parts = [
            new GeminiPart
            {
                Text = "{systemPrompt}"
            }
        ]
    };

    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } =
    [
        new()
        {
            Parts = [
                new GeminiPart
                {
                    Text = "{userMessage}"
                }
            ]
        }
    ];
}
