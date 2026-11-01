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
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class AnthropicServiceTests
{
    private const string ForcedToolChoiceError =
        "{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\"," +
        "\"message\":\"tool_choice of type 'tool' is not supported for this model.\"}}";

    private readonly Mock<ISettingService> _settingsMock = new();
    private readonly StubHttpMessageHandler _handler = new();
    private readonly AnthropicService _service;

    // The service remembers per model which request form works, in static state. Every test uses its own model name,
    // so the tests cannot influence each other.
    private readonly string _model = "claude-test-" + Guid.NewGuid().ToString("N");

    private readonly List<BatchSubtitleItem> _batch =
    [
        new() { Position = 1, Line = "Hello" },
        new() { Position = 2, Line = "World" }
    ];

    public AnthropicServiceTests()
    {
        _settingsMock.Setup(s => s.GetEncryptedSetting(It.IsAny<string>()))
            .ReturnsAsync("test-api-key");
        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                { SettingKeys.Translation.Anthropic.Model, _model },
                { SettingKeys.Translation.Anthropic.Version, "2023-06-01" },
                { SettingKeys.Translation.Anthropic.RequestTemplate, "" },
                { SettingKeys.Translation.AiPrompt, "Translate from {sourceLanguage} to {targetLanguage}." },
                { SettingKeys.Translation.AiUserPrompt, "{lineToTranslate}" },
                { SettingKeys.Translation.RequestTimeout, "1" },
                { SettingKeys.Translation.MaxRetries, "1" },
                { SettingKeys.Translation.RetryDelay, "0" },
                { SettingKeys.Translation.RetryDelayMultiplier, "1" },
                { SettingKeys.Translation.LanguageCodeFormat, "false" }
            });

        _service = new AnthropicService(
            _settingsMock.Object,
            new HttpClient(_handler),
            new Mock<ILogger<AnthropicService>>().Object,
            new LanguageCodeService(),
            new RequestTemplateService());
    }

    [Fact]
    public async Task TranslateBatchAsync_ForcesTheTool_WhenTheModelAcceptsForcedToolUse()
    {
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        var request = Assert.Single(_handler.RequestBodies);
        Assert.Equal("tool", ToolChoiceType(request));
        Assert.False(ToolIsStrict(request));
        Assert.Equal(1024, MaxTokens(request));
    }

    [Fact]
    public async Task TranslateBatchAsync_RetriesWithAutoToolChoice_WhenForcedToolUseIsNotSupported()
    {
        _handler.Enqueue(HttpStatusCode.BadRequest, ForcedToolChoiceError);
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(2, _handler.RequestBodies.Count);
        Assert.Equal("tool", ToolChoiceType(_handler.RequestBodies[0]));

        var retry = _handler.RequestBodies[1];
        Assert.Equal("auto", ToolChoiceType(retry));
        Assert.True(ToolIsStrict(retry));
        Assert.True(MaxTokens(retry) >= 16000);
        Assert.StartsWith("Translate every subtitle item", UserMessage(retry));
    }

    [Fact]
    public async Task TranslateBatchAsync_UsesAutoToolChoiceDirectly_ForLaterBatchesOfTheSameModel()
    {
        _handler.Enqueue(HttpStatusCode.BadRequest, ForcedToolChoiceError);
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));

        await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);
        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(3, _handler.RequestBodies.Count);
        Assert.Equal("auto", ToolChoiceType(_handler.RequestBodies[2]));
    }

    [Fact]
    public async Task TranslateBatchAsync_DoesNotRetry_OnOtherBadRequests()
    {
        _handler.Enqueue(HttpStatusCode.BadRequest,
            "{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\",\"message\":\"max_tokens: too large\"}}");

        await Assert.ThrowsAsync<TranslationException>(() =>
            _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None));

        Assert.Single(_handler.RequestBodies);
    }

    [Fact]
    public async Task TranslateBatchAsync_RetriesWithStrictTool_WhenTranslationsAreReturnedAsAString()
    {
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(JsonSerializer.Serialize(TranslationsArray())));
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(2, _handler.RequestBodies.Count);
        Assert.False(ToolIsStrict(_handler.RequestBodies[0]));
        Assert.True(ToolIsStrict(_handler.RequestBodies[1]));
        Assert.Equal("tool", ToolChoiceType(_handler.RequestBodies[1]));
    }

    [Fact]
    public async Task TranslateBatchAsync_ParsesAJsonString_WhenTheStrictRetryStillReturnsAString()
    {
        var asString = JsonSerializer.Serialize(TranslationsArray());
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(asString));
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(asString));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(2, _handler.RequestBodies.Count);
    }

    [Fact]
    public async Task TranslateBatchAsync_ReadsTheTranslationsFromText_WhenTheModelAnswersWithoutTheTool()
    {
        _handler.Enqueue(HttpStatusCode.BadRequest, ForcedToolChoiceError);
        _handler.Enqueue(HttpStatusCode.OK, TextResponse(
            "Here you go: " + JsonSerializer.Serialize(new { translations = TranslationsArray() })));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(2, _handler.RequestBodies.Count);
    }

    [Fact]
    public async Task TranslateBatchAsync_AsksOnceMoreWithMoreRoom_WhenTheAnswerIsTruncated()
    {
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray(onlyFirst: true), "max_tokens"));
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(2, _handler.RequestBodies.Count);
        Assert.Equal(64000, MaxTokens(_handler.RequestBodies[1]));
    }

    [Fact]
    public async Task TranslateBatchAsync_NeverAcceptsATruncatedAnswer()
    {
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray(onlyFirst: true), "max_tokens"));
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray(onlyFirst: true), "max_tokens"));

        await Assert.ThrowsAsync<TranslationException>(() =>
            _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None));

        Assert.Equal(2, _handler.RequestBodies.Count);
    }

    [Fact]
    public async Task TranslateBatchAsync_AsksOnceMore_WhenTheModelEndsWithoutTheTool()
    {
        _handler.Enqueue(HttpStatusCode.OK, TextResponse("I translated the lines."));
        _handler.Enqueue(HttpStatusCode.OK, ToolUseResponse(TranslationsArray()));

        var result = await _service.TranslateBatchAsync(_batch, "en", "nl", CancellationToken.None);

        AssertTranslated(result);
        Assert.Equal(2, _handler.RequestBodies.Count);
        Assert.Equal(1024, MaxTokens(_handler.RequestBodies[1]));
    }

    private static object[] TranslationsArray(bool onlyFirst = false) =>
        onlyFirst
            ? new object[] { new { position = 1, line = "Hallo" } }
            : new object[] { new { position = 1, line = "Hallo" }, new { position = 2, line = "Wereld" } };

    private static string ToolUseResponse(object translations, string stopReason = "tool_use") =>
        JsonSerializer.Serialize(new
        {
            content = new object[]
            {
                new
                {
                    type = "tool_use",
                    id = "toolu_test",
                    name = "record_translation_batch",
                    input = new { translations }
                }
            },
            stop_reason = stopReason,
            usage = new { input_tokens = 10, output_tokens = 20 }
        });

    private static string TextResponse(string text) =>
        JsonSerializer.Serialize(new
        {
            content = new object[] { new { type = "text", text } },
            stop_reason = "end_turn",
            usage = new { input_tokens = 10, output_tokens = 20 }
        });

    private static void AssertTranslated(Dictionary<int, string> result)
    {
        Assert.Equal(2, result.Count);
        Assert.Equal("Hallo", result[1]);
        Assert.Equal("Wereld", result[2]);
    }

    private static string? ToolChoiceType(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);
        return document.RootElement.GetProperty("tool_choice").GetProperty("type").GetString();
    }

    private static bool ToolIsStrict(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);
        var tool = document.RootElement.GetProperty("tools")[0];
        return tool.TryGetProperty("strict", out var strict) && strict.ValueKind == JsonValueKind.True;
    }

    private static int MaxTokens(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);
        return document.RootElement.GetProperty("max_tokens").GetInt32();
    }

    private static string UserMessage(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);
        return document.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() ?? string.Empty;
    }

    /// <summary>
    /// Returns the queued responses in order and keeps the body of every request, so a test can check what was sent.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

        public List<string> RequestBodies { get; } = [];

        public void Enqueue(HttpStatusCode status, string body) => _responses.Enqueue((status, body));

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBodies.Add(request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("The service sent more requests than the test expected.");
            }

            var (status, body) = _responses.Dequeue();
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}
