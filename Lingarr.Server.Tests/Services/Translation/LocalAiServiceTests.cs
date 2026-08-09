using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models.Batch;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class LocalAiServiceTests
{
    private const string GenerateEndpoint = "http://localhost:11434/api/generate";
    private const string ChatEndpoint = "http://localhost:11434/v1/chat/completions";

    /// <summary>
    /// A response where the model left an unescaped quote inside a translated line, the failure
    /// mode reported in issue #456 for small local models such as gemma4:e4b.
    /// </summary>
    private const string MalformedJson =
        "[{\"position\":1,\"line\":\"Hola\"},{\"position\":2,\"line\":\"Él dijo \"adiós\" y se fue\"}]";

    private const string ValidJson =
        "[{\"position\":1,\"line\":\"Hola\"},{\"position\":2,\"line\":\"Mundo\"}]";

    private readonly Mock<ISettingService> _settingsMock;
    private readonly Mock<ILogger<LocalAiService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly LocalAiService _service;

    public LocalAiServiceTests()
    {
        _settingsMock = new Mock<ISettingService>();
        _loggerMock = new Mock<ILogger<LocalAiService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _settingsMock.Setup(settingService => settingService.GetEncryptedSetting(It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        _service = new LocalAiService(
            _settingsMock.Object,
            new HttpClient(_httpMessageHandlerMock.Object),
            _loggerMock.Object,
            new LanguageCodeService(),
            new RequestTemplateService());
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldRetry_WhenGenerateApiReturnsMalformedJson()
    {
        // Arrange
        UseSettings(GenerateEndpoint);
        SetupResponseSequence(GenerateResponse(MalformedJson), GenerateResponse(ValidJson));

        // Act
        var result = await _service.TranslateBatchAsync(Batch(), "en", "es", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Hola", result[1]);
        Assert.Equal("Mundo", result[2]);
        VerifyRequestsSent(2);
        VerifyWarningLogged("returned an unparsable response", Times.Once());
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldThrow_WhenGenerateApiKeepsReturningMalformedJson()
    {
        // Arrange
        UseSettings(GenerateEndpoint);
        SetupResponse(() => GenerateResponse(MalformedJson));

        // Act
        var exception = await Assert.ThrowsAsync<TranslationException>(
            () => _service.TranslateBatchAsync(Batch(), "en", "es", CancellationToken.None));

        // Assert
        Assert.Contains("Retry limit reached", exception.Message);
        Assert.IsType<TranslationParseException>(exception.InnerException);
        VerifyRequestsSent(3); // MaxRetries
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldRetry_WhenChatApiFallbackReturnsMalformedJson()
    {
        // Arrange
        UseSettings(ChatEndpoint);

        // The chat endpoint tries structured output first and falls back to plain JSON parsing,
        // so a failing attempt sends two requests before the retry kicks in.
        SetupResponseSequence(
            ChatResponse(MalformedJson),
            ChatResponse(MalformedJson),
            ChatResponse("{\"translations\":" + ValidJson + "}"));

        // Act
        var result = await _service.TranslateBatchAsync(Batch(), "en", "es", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Mundo", result[2]);
        VerifyRequestsSent(3);
        VerifyWarningLogged("returned an unparsable response", Times.Once());
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldNotRetry_WhenRequestFailsWithNonRetryableStatus()
    {
        // Arrange
        UseSettings(GenerateEndpoint);
        SetupResponse(() => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("{\"error\":\"model not found\"}", Encoding.UTF8, "application/json")
        });

        // Act
        var exception = await Assert.ThrowsAsync<TranslationException>(
            () => _service.TranslateBatchAsync(Batch(), "en", "es", CancellationToken.None));

        // Assert
        Assert.Contains("Unexpected error", exception.Message);
        VerifyRequestsSent(1);
    }

    private static List<BatchSubtitleItem> Batch() =>
    [
        new() { Position = 1, Line = "Hello" },
        new() { Position = 2, Line = "World" }
    ];

    private static HttpResponseMessage GenerateResponse(string translatedJson) =>
        JsonResponse(new { model = "gemma4:e4b", response = translatedJson, done = true });

    private static HttpResponseMessage ChatResponse(string translatedJson) =>
        JsonResponse(new { choices = new[] { new { message = new { content = translatedJson } } } });

    private static HttpResponseMessage JsonResponse(object body) => new()
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
    };

    private void UseSettings(string endpoint)
    {
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.LocalAi.Model, "gemma4:e4b" },
            { SettingKeys.Translation.LocalAi.Endpoint, endpoint },
            { SettingKeys.Translation.LocalAi.ChatRequestTemplate, "" },
            { SettingKeys.Translation.LocalAi.GenerateRequestTemplate, "" },
            { SettingKeys.Translation.AiPrompt, "Translate from {sourceLanguage} to {targetLanguage}" },
            { SettingKeys.Translation.AiUserPrompt, "{lineToTranslate}" },
            { SettingKeys.Translation.RequestTimeout, "5" },
            { SettingKeys.Translation.MaxRetries, "3" },
            { SettingKeys.Translation.RetryDelay, "0" }, // No delay to keep the tests fast
            { SettingKeys.Translation.RetryDelayMultiplier, "1" },
            { SettingKeys.Translation.LanguageCodeFormat, "false" }
        };

        _settingsMock.Setup(settingService => settingService.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);
    }

    /// <summary>
    /// Answers every request with a freshly built response, so an attempt never reads content
    /// that a previous attempt already consumed.
    /// </summary>
    private void SetupResponse(Func<HttpResponseMessage> responseFactory)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(responseFactory()));
    }

    private void SetupResponseSequence(params HttpResponseMessage[] responses)
    {
        var sequence = _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var response in responses)
        {
            sequence = sequence.ReturnsAsync(response);
        }
    }

    private void VerifyRequestsSent(int count)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(count),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyWarningLogged(string message, Times times)
    {
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
