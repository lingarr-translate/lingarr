using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations.Translation;

public class GeminiResponse
{
    [JsonPropertyName("candidates")] 
    public List<Candidate> Candidates { get; set; } = new();

    public class Candidate
    {
        [JsonPropertyName("content")] 
        public Content? Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")] 
        public List<Part> Parts { get; set; } = new();
    }

    public class Part
    {
        [JsonPropertyName("text")] 
        public string Text { get; set; } = string.Empty;
    }
}