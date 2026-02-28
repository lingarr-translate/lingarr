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
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ITestOutputHelper _output;

    public GoogleGeminiServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _settingsMock = new Mock<ISettingService>();
        _loggerMock = new Mock<ILogger<GoogleGeminiService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _settingsMock.Setup(settingService => settingService.GetEncryptedSetting(It.IsAny<string>()))
            .ReturnsAsync("test-api-key");

        var languageCodeService = new LanguageCodeService(NullLogger<LanguageCodeService>.Instance);
        var requestTemplateService = new RequestTemplateService();
        _service = new GoogleGeminiService(
            _settingsMock.Object,
            _httpClient,
            _loggerMock.Object,
            languageCodeService,
            requestTemplateService);
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
            { SettingKeys.Translation.Gemini.RequestTemplate, "" },
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully repaired the truncated JSON")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldRepairJson_WhenTruncatedInMiddleOfNumber()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.Gemini.ApiKey, "test-api-key" },
            { SettingKeys.Translation.Gemini.Model, "gemini-pro" },
            { SettingKeys.Translation.AiPrompt, "Translate this." },
            { SettingKeys.Translation.AiContextPrompt, "Context." },
            { SettingKeys.Translation.AiContextPromptEnabled, "false" },
            { SettingKeys.Translation.Gemini.RequestTemplate, "" },
            { SettingKeys.Translation.RequestTimeout, "30" },
            { SettingKeys.Translation.MaxRetries, "3" },
            { SettingKeys.Translation.RetryDelay, "1000" },
            { SettingKeys.Translation.RetryDelayMultiplier, "2" }
        };

        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);

        var batch = new List<BatchSubtitleItem>
        {
            new() { Position = 1, Line = "Line 1" },
            new() { Position = 2, Line = "Line 2" }
        };

        // Simulate truncation happening right inside a number, like in Issue #204
        // ...{"position":43
        var truncatedJson = "[{\"position\":1,\"line\":\"Line 1\"},{\"position\":2";
        
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
        Assert.Single(result); // Should contain only the first item
        Assert.Equal("Line 1", result[1]);
    }

    [Fact]
    public async Task TranslateBatchAsync_Integration_LargeBatch_ShouldHandleTruncation()
    {
        // This test reproduces Issue #204 with the REAL API
        // Skips automatically in CI/CD if no API key is set
        var apiKey = Environment.GetEnvironmentVariable("LINGARR_TEST_GEMINI_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("⏭️  Skipping integration test - no API key set");
            return; 
        }

        var model = Environment.GetEnvironmentVariable("LINGARR_TEST_GEMINI_MODEL") ?? "gemini-2.5-flash";
        
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.Gemini.ApiKey, apiKey },
            { SettingKeys.Translation.Gemini.Model, model },
            { SettingKeys.Translation.AiPrompt, "Translate the following subtitles to Spanish. Return JSON." },
            { SettingKeys.Translation.AiContextPrompt, "" },
            { SettingKeys.Translation.AiContextPromptEnabled, "false" },
            { SettingKeys.Translation.Gemini.RequestTemplate, "" },
            { SettingKeys.Translation.RequestTimeout, "120" },
            { SettingKeys.Translation.MaxRetries, "3" },
            { SettingKeys.Translation.RetryDelay, "1000" },
            { SettingKeys.Translation.RetryDelayMultiplier, "2" }
        };

        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);
        
        _settingsMock.Setup(s => s.GetSetting(SettingKeys.Translation.Gemini.ApiKey))
            .ReturnsAsync(apiKey);

        var realHttpClient = new HttpClient();
        var languageCodeService = new LanguageCodeService(NullLogger<LanguageCodeService>.Instance);
        var requestTemplateService2 = new RequestTemplateService();
        var service = new GoogleGeminiService(
            _settingsMock.Object,
            realHttpClient,
            _loggerMock.Object,
            languageCodeService,
            requestTemplateService2
        );

        // Create a LARGE batch similar to the bug report (150 items to trigger truncation)
        var batch = new List<BatchSubtitleItem>();
        for (int i = 1; i <= 150; i++)
        {
            batch.Add(new BatchSubtitleItem 
            { 
                Position = i, 
                Line = $"This is a longer subtitle line number {i} that contains more text to simulate real subtitle content and increase the response size which could trigger JSON truncation at token limits." 
            });
        }

        _output.WriteLine($"🔬 Testing large batch translation with {batch.Count} items...");
        _output.WriteLine($"📏 Total input size: ~{batch.Sum(b => b.Line.Length)} characters");

        try 
        {
            var result = await service.TranslateBatchAsync(batch, "en", "es", CancellationToken.None);

            // Assertions
            Assert.NotNull(result);
            Assert.True(result.Count > 0, "Should return at least some translations");
            
            _output.WriteLine($"✅ Large batch translation succeeded!");
            _output.WriteLine($"📊 Translated {result.Count} out of {batch.Count} items");
            
            // Log sample translations
            _output.WriteLine($"📝 Sample translations (first 3):");
            foreach (var kvp in result.Take(3))
            {
                _output.WriteLine($"  [{kvp.Key}] {kvp.Value.Substring(0, Math.Min(60, kvp.Value.Length))}...");
            }
            
            if (result.Count < batch.Count)
            {
                _output.WriteLine($"⚠️  Note: {batch.Count - result.Count} items were truncated but handled gracefully");
            }
            else
            {
                _output.WriteLine($"🎉 All {result.Count} items translated successfully!");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ TEST FAILED: {ex.Message}");
            throw;
        }
        finally
        {
            realHttpClient.Dispose();
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateAsync_ShouldRetry_WhenRetryableStatusCode(HttpStatusCode statusCode)
    {
        // Arrange
        var settings = GetDefaultSettings();
        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>())).ReturnsAsync(settings);

        var successResponse = new { candidates = new[] { new { content = new { parts = new[] { new { text = "Translated Text" } } } } } };
        var successContent = JsonSerializer.Serialize(successResponse);

        _httpMessageHandlerMock.Protected().SetupSequence<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode })
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(successContent, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await _service.TranslateAsync("Source Text", "en", "es", null, null, CancellationToken.None);

        // Assert
        Assert.Equal("Translated Text", result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrying")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateBatchAsync_ShouldRetry_WhenRetryableStatusCode(HttpStatusCode statusCode)
    {
        // Arrange
        var settings = GetDefaultSettings();
        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>())).ReturnsAsync(settings);

        var batch = new List<BatchSubtitleItem> { new() { Position = 1, Line = "Hello" } };
        var successBatchResponse = new { candidates = new[] { new { content = new { parts = new[] { new { text = "[{\"position\":1,\"line\":\"Hola\"}]" } } } } } };
        var successContent = JsonSerializer.Serialize(successBatchResponse);

        _httpMessageHandlerMock.Protected().SetupSequence<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode })
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(successContent, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await _service.TranslateBatchAsync(batch, "en", "es", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hola", result[1]);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateAsync_ShouldThrow_WhenRetriesExhausted(HttpStatusCode statusCode)
    {
        // Arrange
        var settings = GetDefaultSettings();
        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>())).ReturnsAsync(settings);

        // Return the error status for every attempt (MaxRetries = 3)
        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TranslationException>(
            () => _service.TranslateAsync("Source Text", "en", "es", null, null, CancellationToken.None));

        Assert.Contains("Retry limit reached", ex.Message);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateBatchAsync_ShouldThrow_WhenRetriesExhausted(HttpStatusCode statusCode)
    {
        // Arrange
        var settings = GetDefaultSettings();
        _settingsMock.Setup(s => s.GetSettings(It.IsAny<IEnumerable<string>>())).ReturnsAsync(settings);

        var batch = new List<BatchSubtitleItem> { new() { Position = 1, Line = "Hello" } };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TranslationException>(
            () => _service.TranslateBatchAsync(batch, "en", "es", CancellationToken.None));

        Assert.Contains("Retry limit reached", ex.Message);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    // Helper to keep the tests clean
    private Dictionary<string, string> GetDefaultSettings()
    {
        return new Dictionary<string, string>
        {
            { SettingKeys.Translation.Gemini.ApiKey, "test-api-key" },
            { SettingKeys.Translation.Gemini.Model, "gemini-pro" },
            { SettingKeys.Translation.AiPrompt, "Prompt" },
            { SettingKeys.Translation.AiContextPrompt, "" },
            { SettingKeys.Translation.AiContextPromptEnabled, "false" },
            { SettingKeys.Translation.Gemini.RequestTemplate, "" },
            { SettingKeys.Translation.RequestTimeout, "5" },
            { SettingKeys.Translation.MaxRetries, "3" },
            { SettingKeys.Translation.RetryDelay, "1" }, // Short delay for fast tests
            { SettingKeys.Translation.RetryDelayMultiplier, "1" }
        };
    }
}
