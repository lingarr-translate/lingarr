using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Contracts.Models.Batch;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class AnthropicServiceTests
{
    private const string TranslationsJson =
        "{\"translations\":[{\"position\":1,\"line\":\"Hallo\"},{\"position\":2,\"line\":\"Wereld\"}]}";

    private readonly Mock<ISettingService> _settingsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly AnthropicService _service;

    public AnthropicServiceTests()
    {
        _settingsMock = new Mock<ISettingService>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _settingsMock.Setup(settingService => settingService.GetEncryptedSetting(It.IsAny<string>()))
            .ReturnsAsync("test-api-key");

        _service = new AnthropicService(
            _settingsMock.Object,
            new HttpClient(_httpMessageHandlerMock.Object),
            new Mock<ILogger<AnthropicService>>().Object,
            new LanguageCodeService(),
            new RequestTemplateService());
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldRequestStructuredOutput_InsteadOfForcedTool()
    {
        // Arrange
        UseSettings(requestTemplate: "");
        var requestBodies = CaptureRequestBodies(() => MessagesResponse(TranslationsJson));

        // Act
        var result = await _service.TranslateBatchAsync(Batch(), "en", "nl", CancellationToken.None);

        // Assert
        Assert.Equal("Hallo", result[1]);
        Assert.Equal("Wereld", result[2]);
        var request = Assert.Single(requestBodies);
        using var body = JsonDocument.Parse(request);
        Assert.False(body.RootElement.TryGetProperty("tools", out _));
        Assert.False(body.RootElement.TryGetProperty("tool_choice", out _));
        var format = body.RootElement.GetProperty("output_config").GetProperty("format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.True(format.GetProperty("schema").GetProperty("properties").TryGetProperty("translations", out _));
        Assert.Equal(16000, body.RootElement.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldKeepHigherMaxTokensAndOutputConfig_FromRequestTemplate()
    {
        // Arrange
        UseSettings(requestTemplate:
            "{\"model\":\"{model}\",\"max_tokens\":32000,\"system\":\"{systemPrompt}\"," +
            "\"thinking\":{\"type\":\"adaptive\"},\"output_config\":{\"effort\":\"high\"}," +
            "\"messages\":[{\"role\":\"user\",\"content\":\"{userMessage}\"}]}");
        var requestBodies = CaptureRequestBodies(() => MessagesResponse(TranslationsJson));

        // Act
        var result = await _service.TranslateBatchAsync(Batch(), "en", "nl", CancellationToken.None);

        // Assert
        Assert.Equal("Wereld", result[2]);
        var request = Assert.Single(requestBodies);
        using var body = JsonDocument.Parse(request);
        Assert.Equal(32000, body.RootElement.GetProperty("max_tokens").GetInt32());
        var outputConfig = body.RootElement.GetProperty("output_config");
        Assert.Equal("high", outputConfig.GetProperty("effort").GetString());
        Assert.Equal("json_schema", outputConfig.GetProperty("format").GetProperty("type").GetString());
    }

    private static List<BatchSubtitleItem> Batch() =>
    [
        new() { Position = 1, Line = "Hello" },
        new() { Position = 2, Line = "World" }
    ];

    /// <summary>
    /// A Messages API response with a thinking block before the text block, as returned with thinking enabled.
    /// </summary>
    private static HttpResponseMessage MessagesResponse(string text) => new()
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(JsonSerializer.Serialize(new
        {
            content = new object[]
            {
                new { type = "thinking", thinking = "...", signature = "sig" },
                new { type = "text", text }
            },
            stop_reason = "end_turn"
        }), Encoding.UTF8, "application/json")
    };

    private void UseSettings(string requestTemplate)
    {
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.Anthropic.Model, "claude-sonnet-5-5" },
            { SettingKeys.Translation.Anthropic.Version, "2023-06-01" },
            { SettingKeys.Translation.Anthropic.RequestTemplate, requestTemplate },
            { SettingKeys.Translation.AiPrompt, "Translate from {sourceLanguage} to {targetLanguage}" },
            { SettingKeys.Translation.AiUserPrompt, "{lineToTranslate}" },
            { SettingKeys.Translation.RequestTimeout, "5" },
            { SettingKeys.Translation.MaxRetries, "1" },
            { SettingKeys.Translation.RetryDelay, "0" },
            { SettingKeys.Translation.RetryDelayMultiplier, "1" },
            { SettingKeys.Translation.LanguageCodeFormat, "false" }
        };

        _settingsMock.Setup(settingService => settingService.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);
    }

    private List<string> CaptureRequestBodies(Func<HttpResponseMessage> responseFactory)
    {
        var requestBodies = new List<string>();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                requestBodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()))
            .Returns(() => Task.FromResult(responseFactory()));
        return requestBodies;
    }
}
