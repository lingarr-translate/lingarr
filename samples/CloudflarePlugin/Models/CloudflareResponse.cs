using System.Text.Json.Serialization;

namespace Lingarr.Plugin.Cloudflare.Models;

public class CloudflareResponse
{
    [JsonPropertyName("result")]
    public TranslationResult? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errors")]
    public List<ApiError> Errors { get; set; } = new();

    public class TranslationResult
    {
        [JsonPropertyName("translated_text")]
        public string TranslatedText { get; set; } = string.Empty;
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
