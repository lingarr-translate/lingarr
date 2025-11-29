using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class GoogleGeminiServiceTests
{
    private readonly Mock<ISettingService> _settingsMock;
    private readonly Mock<ILogger<GoogleGeminiService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly GoogleGeminiService _service;

    public GoogleGeminiServiceTests()
    {
        _settingsMock = new Mock<ISettingService>();
        _loggerMock = new Mock<ILogger<GoogleGeminiService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _service = new GoogleGeminiService(
            _settingsMock.Object,
            _httpClient,
            _loggerMock.Object);
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldRepairTruncatedJson_WhenResponseIsTruncated()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.Gemini.ApiKey, "test-api-key" },
            { SettingKeys.Translation.Gemini.Model, "gemini-pro" },
            { SettingKeys.Translation.AiPrompt, "Translate this." },
            { SettingKeys.Translation.AiContextPrompt, "Context." },
            { SettingKeys.Translation.AiContextPromptEnabled, "false" },
            { SettingKeys.Translation.CustomAiParameters, "{}" },
            { SettingKeys.Translation.RequestTimeout, "30" },
            { SettingKeys.Translation.MaxRetries, "3" },
            { SettingKeys.Translation.RetryDelay, "1000" },
            { SettingKeys.Translation.RetryDelayMultiplier, "2" }
        };

        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);

        var batch = new List<BatchSubtitleItem>
        {
            new() { Position = 1, Line = "Hello" },
            new() { Position = 2, Line = "World" }
        };

        // Truncated JSON response
        // Full response would be: [{"position":1,"line":"Hola"},{"position":2,"line":"Mundo"}]
        // Truncated: [{"position":1,"line":"Hola"},{"position":2,"line":"Mun
        var truncatedJson = "[{\"position\":1,\"line\":\"Hola\"},{\"position\":2,\"line\":\"Mun";
        
        var geminiResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = truncatedJson }
                        }
                    }
                }
            }
        };

        var responseContent = JsonSerializer.Serialize(geminiResponse);
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.TranslateBatchAsync(batch, "en", "es", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Should contain only the first item which was successfully repaired
        Assert.Equal("Hola", result[1]);
        
        // Verify that warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully repaired truncated JSON")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify maxOutputTokens was sent
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Content!.ReadAsStringAsync().Result.Contains("\"maxOutputTokens\":8192")),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
