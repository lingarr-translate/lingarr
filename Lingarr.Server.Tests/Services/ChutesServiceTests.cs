using System;
using System.Collections.Generic;
using System.Linq;
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
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class ChutesServiceTests
{
    [Fact]
    public async Task TranslateBatchAsync_FiltersContextOnlyItemsAndIncludesContextInstruction()
    {
        // Arrange
        var batchInstruction = "Do not translate context-only lines.";
        var baseSettings = new Dictionary<string, string>
        {
            [SettingKeys.Translation.Chutes.Model] = "test-model",
            [SettingKeys.Translation.Chutes.ApiKey] = "secret",
            [SettingKeys.Translation.AiContextPromptEnabled] = "true",
            [SettingKeys.Translation.AiContextPrompt] = "Context before: {contextBefore}\nLine: {lineToTranslate}\nAfter: {contextAfter}",
            [SettingKeys.Translation.AiBatchContextInstruction] = batchInstruction,
            [SettingKeys.Translation.CustomAiParameters] = "[]",
            [SettingKeys.Translation.AiPrompt] = "Translate from {sourceLanguage} to {targetLanguage}",
            [SettingKeys.Translation.RequestTimeout] = "1",
            [SettingKeys.Translation.MaxRetries] = "2",
            [SettingKeys.Translation.RetryDelay] = "1",
            [SettingKeys.Translation.RetryDelayMultiplier] = "2"
        };

        var settingsMock = new Mock<ISettingService>();
        settingsMock
            .Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> keys) =>
            {
                var dict = new Dictionary<string, string>();
                foreach (var key in keys)
                {
                    dict[key] = baseSettings[key];
                }

                return dict;
            });
        settingsMock
            .Setup(s => s.GetSetting(It.IsAny<string>()))
            .ReturnsAsync((string key) => baseSettings.TryGetValue(key, out var value) ? value : null);

        var loggerMock = new Mock<ILogger<ChutesService>>();

        string? capturedRequestBody = null;
        var handler = new TestHttpMessageHandler(async request =>
        {
            capturedRequestBody = await request.Content!.ReadAsStringAsync();

            var payload = new
            {
                translations = new[]
                {
                    new { position = 5, line = "Context output should be ignored" },
                    new { position = 2, line = "Translated content" }
                }
            };

            var completionResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonSerializer.Serialize(payload)
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(completionResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var sut = new ChutesService(settingsMock.Object, httpClient, loggerMock.Object);

        var batchRequest = new List<BatchSubtitleItem>
        {
            new() { Position = 1, Line = "context-before", IsContextOnly = true },
            new() { Position = 2, Line = "actual line", IsContextOnly = false },
            new() { Position = 3, Line = "context-after", IsContextOnly = true }
        };

        // Act
        var result = await sut.TranslateBatchAsync(batchRequest, "en", "es", CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequestBody);
        using var requestDoc = JsonDocument.Parse(capturedRequestBody!);
        var messages = requestDoc.RootElement.GetProperty("messages");
        var systemPrompt = messages[0].GetProperty("content").GetString();
        var userPayload = messages[1].GetProperty("content").GetString();

        Assert.Contains(batchInstruction, systemPrompt, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(userPayload));

        using var batchDoc = JsonDocument.Parse(userPayload!);
        var containsContextFlag = batchDoc.RootElement
            .EnumerateArray()
            .Any(element =>
                element.TryGetProperty("isContextOnly", out var flag) &&
                flag.GetBoolean());
        Assert.True(containsContextFlag);

        Assert.Single(result);
        Assert.Equal("Translated content", result[2]);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
