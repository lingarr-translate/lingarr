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

public class MistralServiceTests
{
    private const string TranslationsJson =
        "{\"translations\":[{\"position\":1,\"line\":\"Hallo\"},{\"position\":2,\"line\":\"Wereld\"}]}";

    private readonly Mock<ISettingService> _settingsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly MistralService _service;

    public MistralServiceTests()
    {
        _settingsMock = new Mock<ISettingService>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _settingsMock.Setup(settingService => settingService.GetEncryptedSetting(It.IsAny<string>()))
            .ReturnsAsync("test-api-key");

        _service = new MistralService(
            _settingsMock.Object,
            new HttpClient(_httpMessageHandlerMock.Object),
            new Mock<ILogger<MistralService>>().Object,
            new LanguageCodeService(),
            new RequestTemplateService());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"result\":[]}")]
    [InlineData("{\"translations\":[{\"position\":1}]}")]
    public async Task TranslateBatchAsync_ShouldRetry_WhenResponseCannotBeParsed(string unparsableText)
    {
        // Arrange
        UseSettings();
        var responses = new Queue<string>([unparsableText, TranslationsJson]);
        SetupResponse(() => ChatCompletionResponse(responses.Dequeue()));

        // Act
        var result = await _service.TranslateBatchAsync(Batch(), "en", "nl", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Wereld", result[2]);
        VerifyRequestsSent(2);
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldThrowWithParseException_WhenRetriesExhausted()
    {
        // Arrange
        UseSettings();
        SetupResponse(() => ChatCompletionResponse("not json"));

        // Act
        var exception = await Assert.ThrowsAsync<TranslationException>(
            () => _service.TranslateBatchAsync(Batch(), "en", "nl", TestContext.Current.CancellationToken));

        // Assert
        Assert.IsType<TranslationParseException>(exception.InnerException);
        VerifyRequestsSent(3);
    }

    private static List<BatchSubtitleItem> Batch() =>
    [
        new() { Position = 1, Line = "Hello" },
        new() { Position = 2, Line = "World" }
    ];

    private static HttpResponseMessage ChatCompletionResponse(string content) => new()
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { role = "assistant", content } } }
        }), Encoding.UTF8, "application/json")
    };

    private void UseSettings()
    {
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.Mistral.Model, "mistral-large-latest" },
            { SettingKeys.Translation.Mistral.RequestTemplate, "" },
            { SettingKeys.Translation.AiPrompt, "Translate from {sourceLanguage} to {targetLanguage}" },
            { SettingKeys.Translation.AiUserPrompt, "{lineToTranslate}" },
            { SettingKeys.Translation.RequestTimeout, "5" },
            { SettingKeys.Translation.MaxRetries, "3" },
            { SettingKeys.Translation.RetryDelay, "0" },
            { SettingKeys.Translation.RetryDelayMultiplier, "1" },
            { SettingKeys.Translation.LanguageCodeFormat, "false" }
        };

        _settingsMock.Setup(settingService => settingService.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);
    }

    private void SetupResponse(Func<HttpResponseMessage> responseFactory)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(responseFactory()));
    }

    private void VerifyRequestsSent(int count)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(count),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
