using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class SubtitleTranslationServiceTests
{
    [Fact]
    public async Task ProcessSubtitleBatchWithContext_PassesContextAndUpdatesTranslations()
    {
        // Arrange
        var translationServiceMock = new Mock<ITranslationService>();
        var batchTranslationServiceMock = new Mock<IBatchTranslationService>();
        List<BatchSubtitleItem>? capturedBatch = null;

        batchTranslationServiceMock
            .Setup(s => s.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                "en",
                "es",
                It.IsAny<CancellationToken>()))
            .Callback<List<BatchSubtitleItem>, string, string, CancellationToken>((batch, _, _, _) =>
            {
                capturedBatch = batch;
            })
            .ReturnsAsync(new Dictionary<int, string>
            {
                { 1, "Translated line one" },
                { 2, "Translated line two" }
            });

        var loggerMock = new Mock<ILogger<SubtitleTranslationService>>();
        var service = new SubtitleTranslationService(translationServiceMock.Object, loggerMock.Object);

        var subtitles = new List<SubtitleItem>
        {
            new()
            {
                Position = 1,
                Lines = ["Line one"],
                PlaintextLines = ["Line one"]
            },
            new()
            {
                Position = 2,
                Lines = ["Line two"],
                PlaintextLines = ["Line two"]
            }
        };

        var contextBefore = new List<BatchSubtitleItem>
        {
            new()
            {
                Position = 100,
                Line = "Context before",
                IsContextOnly = true
            }
        };

        var contextAfter = new List<BatchSubtitleItem>
        {
            new()
            {
                Position = 200,
                Line = "Context after",
                IsContextOnly = true
            }
        };

        // Act
        await service.ProcessSubtitleBatchWithContext(
            subtitles,
            batchTranslationServiceMock.Object,
            "en",
            "es",
            stripSubtitleFormatting: false,
            contextLinesBefore: contextBefore,
            contextLinesAfter: contextAfter,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(capturedBatch);
        var batch = capturedBatch!;
        Assert.Equal(4, batch.Count);
        Assert.True(batch[0].IsContextOnly);
        Assert.Equal(100, batch[0].Position);
        Assert.False(batch[1].IsContextOnly);
        Assert.Equal(1, batch[1].Position);
        Assert.True(batch[^1].IsContextOnly);
        Assert.Equal(200, batch[^1].Position);

        Assert.Equal("Translated line one", string.Join(' ', subtitles[0].TranslatedLines));
        Assert.Equal("Translated line two", string.Join(' ', subtitles[1].TranslatedLines));
    }
}
