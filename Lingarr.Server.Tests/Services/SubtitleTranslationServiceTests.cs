using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class SubtitleTranslationServiceTests
{
    private static TranslationRequest NewRequest() => new()
    {
        Title = "test",
        SourceLanguage = "en",
        TargetLanguage = "es",
        MediaType = MediaType.Movie,
        Status = TranslationStatus.InProgress
    };

    private static SubtitleItem Subtitle(int position, params string[] lines) => new()
    {
        Position = position,
        Lines = lines.ToList(),
        PlaintextLines = lines.ToList()
    };

    private sealed class PerLineHarness
    {
        public Mock<ITranslationService> TranslationServiceMock { get; init; } = null!;
        public Mock<IProgressService> ProgressServiceMock { get; init; } = null!;
        public SubtitleTranslationService Service { get; init; } = null!;
    }

    private sealed class BatchHarness
    {
        public Mock<IProgressService> ProgressServiceMock { get; init; } = null!;
        public SubtitleTranslationService Service { get; init; } = null!;
    }

    private static PerLineHarness CreatePerLineHarness(Func<string, string> translate)
    {
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock
            .Setup(t => t.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string source, string target, CancellationToken _) => new LanguagePair { Source = source, Target = target, Tier = MatchTier.Exact });
        translationServiceMock
            .Setup(t => t.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) => translate(text));

        var progressServiceMock = new Mock<IProgressService>();
        progressServiceMock
            .Setup(p => p.Emit(It.IsAny<TranslationRequest>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        progressServiceMock
            .Setup(progressService => progressService.EmitLine(
                It.IsAny<TranslationRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<LanguagePair?>()))
            .Returns(Task.CompletedTask);
        progressServiceMock
            .Setup(p => p.EmitLines(It.IsAny<TranslationRequest>(), It.IsAny<List<TranslatedLineData>>()))
            .Returns(Task.CompletedTask);

        return new PerLineHarness
        {
            TranslationServiceMock = translationServiceMock,
            ProgressServiceMock = progressServiceMock,
            Service = new SubtitleTranslationService(
                [new TranslationServiceEntry("test", translationServiceMock.Object, null)],
                NullLogger.Instance,
                progressServiceMock.Object)
        };
    }

    private static BatchHarness CreateBatchHarness(Func<List<BatchSubtitleItem>, Dictionary<int, string>> batchTranslate)
    {
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock
            .Setup(t => t.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string source, string target, CancellationToken _) => new LanguagePair { Source = source, Target = target, Tier = MatchTier.Exact });
        var batchTranslationServiceMock = new Mock<IBatchTranslationService>();
        batchTranslationServiceMock
            .Setup(b => b.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<BatchSubtitleItem> items, string _, string _, CancellationToken _) => batchTranslate(items));

        var progressServiceMock = new Mock<IProgressService>();
        progressServiceMock
            .Setup(p => p.Emit(It.IsAny<TranslationRequest>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        progressServiceMock
            .Setup(p => p.EmitLines(It.IsAny<TranslationRequest>(), It.IsAny<List<TranslatedLineData>>()))
            .Returns(Task.CompletedTask);

        return new BatchHarness
        {
            ProgressServiceMock = progressServiceMock,
            Service = new SubtitleTranslationService(
                [new TranslationServiceEntry("test", translationServiceMock.Object, batchTranslationServiceMock.Object)],
                NullLogger.Instance,
                progressServiceMock.Object)
        };
    }

    #region TranslateSubtitles Tests

    [Fact]
    public async Task TranslateSubtitles_SingleLine_TranslatesAsOneCallAndReturnsOneLine()
    {
        // Arrange
        var harness = CreatePerLineHarness(_ => "hola");
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello") };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["hola"], subtitles[0].TranslatedLines);
        harness.TranslationServiceMock.Verify(
            t => t.TranslateAsync("hello", "en", "es", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateSubtitles_MultiLinePreserveOff_MergesIntoOneCallAndReturnsOneLine()
    {
        // Arrange
        var captured = new List<string>();
        var harness = CreatePerLineHarness(text =>
        {
            captured.Add(text);
            return "hola mundo";
        });
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["hello world"], captured);
        Assert.Equal(["hola mundo"], subtitles[0].TranslatedLines);
        harness.TranslationServiceMock.Verify(
            t => t.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateSubtitles_MultiLinePreserveOffStripOn_RewrapsLongTranslation()
    {
        // Arrange - multi-line merged input with a long translation; rewrap kicks in because strip is on
        var harness = CreatePerLineHarness(_ => "Esta es una traducción bastante larga que sin duda excederá el límite de cuarenta y dos caracteres");
        var subtitles = new List<SubtitleItem> { Subtitle(1, "line a", "line b") };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: true,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        var translated = subtitles[0].TranslatedLines;
        Assert.True(translated.Count > 1, "Expected the long translation to be wrapped into multiple lines");
        Assert.All(translated, line => Assert.True(line.Length <= 42, $"Line exceeded max length: '{line}'"));
    }

    [Fact]
    public async Task TranslateSubtitles_MultiLinePreserveOn_TranslatesEachLineSeparately()
    {
        // Arrange
        var captured = new List<string>();
        var harness = CreatePerLineHarness(text =>
        {
            captured.Add(text);
            return text == "hello" ? "hola" : "mundo";
        });
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: true,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["hello", "world"], captured);
        Assert.Equal(["hola", "mundo"], subtitles[0].TranslatedLines);
        harness.TranslationServiceMock.Verify(
            t => t.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task TranslateSubtitles_PreserveOnPerLineTranslationWithEmbeddedNewline_KeepsModelOutputVerbatim()
    {
        // Arrange - a per-line translation contains a literal '\n'. We must NOT round-trip through ToSubtitleLines
        // (which would split on '\n' and trip the count-mismatch fallback). Output should be stored as the model returned.
        var harness = CreatePerLineHarness(text => text == "hello" ? "ho\nla" : "mundo");
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: true,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["ho\nla", "mundo"], subtitles[0].TranslatedLines);
    }

    [Fact]
    public async Task TranslateSubtitles_WhitespaceLine_SkipsTranslationAndPassesThrough()
    {
        // Arrange
        var translatedCalls = 0;
        var harness = CreatePerLineHarness(_ =>
        {
            translatedCalls++;
            return "mundo";
        });
        var subtitles = new List<SubtitleItem> { Subtitle(1, "", "world") };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: true,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["", "mundo"], subtitles[0].TranslatedLines);
        Assert.Equal(1, translatedCalls);
    }

    // ===== Per-file dedup =====
    // Many fansub .ass files stack multiple Dialogue lines at the same timestamp
    // with byte-identical plaintext (shadow/glow/border/main rendered as separate
    // entries). The service caches by (StartTime, EndTime, plaintext) and reuses
    // the translation for the duplicates.

    [Fact]
    public async Task TranslateSubtitles_StackedDuplicates_TranslatesOnceAndReusesResult()
    {
        // Arrange - four entries with the same (Start, End, plaintext): the layered shadow/glow/border/main case
        var captured = new List<string>();
        var harness = CreatePerLineHarness(text => { captured.Add(text); return "hola"; });
        var subtitles = new List<SubtitleItem>
        {
            new() { Position = 1, StartTime = 1000, EndTime = 2000, Lines = ["Hello"], PlaintextLines = ["Hello"] },
            new() { Position = 2, StartTime = 1000, EndTime = 2000, Lines = ["Hello"], PlaintextLines = ["Hello"] },
            new() { Position = 3, StartTime = 1000, EndTime = 2000, Lines = ["Hello"], PlaintextLines = ["Hello"] },
            new() { Position = 4, StartTime = 1000, EndTime = 2000, Lines = ["Hello"], PlaintextLines = ["Hello"] }
        };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Single(captured);
        Assert.All(subtitles, s => Assert.Equal(["hola"], s.TranslatedLines));
    }

    [Fact]
    public async Task TranslateSubtitles_SameTextDifferentTimes_TranslatesEachIndependently()
    {
        // Arrange - same plaintext at different timestamps are different scenes; each must be translated
        var captured = new List<string>();
        var harness = CreatePerLineHarness(text => { captured.Add(text); return "si"; });
        var subtitles = new List<SubtitleItem>
        {
            new() { Position = 1, StartTime = 1000, EndTime = 2000, Lines = ["Yes"], PlaintextLines = ["Yes"] },
            new() { Position = 2, StartTime = 5000, EndTime = 6000, Lines = ["Yes"], PlaintextLines = ["Yes"] },
            new() { Position = 3, StartTime = 9000, EndTime = 10000, Lines = ["Yes"], PlaintextLines = ["Yes"] }
        };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(3, captured.Count);
    }

    [Fact]
    public async Task TranslateSubtitles_NoDuplicates_TranslatesEvery()
    {
        // Arrange - SRT/VTT typical case: every entry unique
        var captured = new List<string>();
        var harness = CreatePerLineHarness(text => { captured.Add(text); return $"tr:{text}"; });
        var subtitles = new List<SubtitleItem>
        {
            new() { Position = 1, StartTime = 1000, EndTime = 2000, Lines = ["Hello"], PlaintextLines = ["Hello"] },
            new() { Position = 2, StartTime = 3000, EndTime = 4000, Lines = ["World"], PlaintextLines = ["World"] },
            new() { Position = 3, StartTime = 5000, EndTime = 6000, Lines = ["How are you?"], PlaintextLines = ["How are you?"] }
        };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["Hello", "World", "How are you?"], captured);
        Assert.Equal(["tr:Hello"], subtitles[0].TranslatedLines);
        Assert.Equal(["tr:World"], subtitles[1].TranslatedLines);
        Assert.Equal(["tr:How are you?"], subtitles[2].TranslatedLines);
    }

    [Fact]
    public async Task TranslateSubtitles_SameTimestampDifferentText_TranslatesEachIndependently()
    {
        // Arrange - overlapping dual-speaker dialogue: same timestamp, different text — both translated
        var captured = new List<string>();
        var harness = CreatePerLineHarness(text => { captured.Add(text); return $"tr:{text}"; });
        var subtitles = new List<SubtitleItem>
        {
            new() { Position = 1, StartTime = 1000, EndTime = 2000, Lines = ["Run!"], PlaintextLines = ["Run!"] },
            new() { Position = 2, StartTime = 1000, EndTime = 2000, Lines = ["Watch out!"], PlaintextLines = ["Watch out!"] }
        };

        // Act
        await harness.Service.TranslateSubtitles(subtitles, NewRequest(),
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        Assert.Equal(["Run!", "Watch out!"], captured);
    }

    #endregion

    #region ProcessSubtitleBatch Tests

    [Fact]
    public async Task ProcessSubtitleBatch_PreserveOff_JoinsWithSpaceAndReturnsSingleLine()
    {
        // Arrange
        List<BatchSubtitleItem> seen = [];
        var harness = CreateBatchHarness(items =>
        {
            seen = items;
            return items.ToDictionary(i => i.Position, _ => "hola mundo");
        });
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.ProcessSubtitleBatch(subtitles,
            "en", "es",
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            CancellationToken.None);

        // Assert
        Assert.Equal("hello world", seen[0].Line);
        Assert.Equal(["hola mundo"], subtitles[0].TranslatedLines);
    }

    [Fact]
    public async Task ProcessSubtitleBatch_PreserveOnMultiLine_JoinsWithNewlineAndSplitsBack()
    {
        // Arrange
        List<BatchSubtitleItem> seen = [];
        var harness = CreateBatchHarness(items =>
        {
            seen = items;
            return items.ToDictionary(i => i.Position, _ => "hola\nmundo");
        });
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.ProcessSubtitleBatch(subtitles,
            "en", "es",
            stripSubtitleFormatting: false,
            preserveLineBreaks: true,
            CancellationToken.None);

        // Assert
        Assert.Equal("hello\nworld", seen[0].Line);
        Assert.Equal(["hola", "mundo"], subtitles[0].TranslatedLines);
    }

    [Fact]
    public async Task ProcessSubtitleBatch_PreserveOnButLineCountMismatch_CollapsesToSingleLine()
    {
        // Arrange - model returns three '\n'-separated lines but source had two; expect fallback to a merged single line
        var harness = CreateBatchHarness(items =>
            items.ToDictionary(i => i.Position, _ => "hola\nmundo\nextra"));
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.ProcessSubtitleBatch(subtitles,
            "en", "es",
            stripSubtitleFormatting: false,
            preserveLineBreaks: true,
            CancellationToken.None);

        // Assert
        Assert.Equal(["hola mundo extra"], subtitles[0].TranslatedLines);
    }

    [Fact]
    public async Task ProcessSubtitleBatch_PreserveOffStripOnMultiLine_RewrapsLongTranslation()
    {
        // Arrange
        var harness = CreateBatchHarness(items =>
            items.ToDictionary(i => i.Position, _ => "Esta es una traducción bastante larga que sin duda excederá el límite de cuarenta y dos caracteres"));
        var subtitles = new List<SubtitleItem> { Subtitle(1, "line a", "line b") };

        // Act
        await harness.Service.ProcessSubtitleBatch(subtitles,
            "en", "es",
            stripSubtitleFormatting: true,
            preserveLineBreaks: false,
            CancellationToken.None);

        // Assert
        var translated = subtitles[0].TranslatedLines;
        Assert.True(translated.Count > 1, "Expected the long translation to be wrapped into multiple lines");
        Assert.All(translated, line => Assert.True(line.Length <= 42, $"Line exceeded max length: '{line}'"));
    }

    [Fact]
    public async Task ProcessSubtitleBatch_MissingTranslation_FallsBackToOriginalLines()
    {
        // Arrange
        var harness = CreateBatchHarness(_ => new Dictionary<int, string>());
        var subtitles = new List<SubtitleItem> { Subtitle(1, "hello", "world") };

        // Act
        await harness.Service.ProcessSubtitleBatch(subtitles,
            "en", "es",
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            CancellationToken.None);

        // Assert
        Assert.Equal(["hello", "world"], subtitles[0].TranslatedLines);
    }

    #endregion

    #region Chain-wide best-match resolution

    private static Mock<ITranslationService> MockService(LanguagePair? pair, Func<string, string> translate)
    {
        var mock = new Mock<ITranslationService>();
        mock.Setup(service => service.GetLanguagePair(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pair);
        mock.Setup(service => service.TranslateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) => translate(text));
        return mock;
    }

    [Fact]
    public async Task TranslateSubtitleLine_PrefersExactMatchOverNeutralFallback()
    {
        // Arrange
        var primary = MockService(new LanguagePair { Source = "en", Target = "zh", Tier = MatchTier.NeutralEquivalent }, _ => "primary");
        var fallback = MockService(new LanguagePair { Source = "en", Target = "zh-TW", Tier = MatchTier.Exact }, _ => "fallback");
        var service = new SubtitleTranslationService(
            [
                new TranslationServiceEntry("primary", primary.Object, null),
                new TranslationServiceEntry("fallback", fallback.Object, null)
            ],
            NullLogger.Instance);

        // Act
        var (translation, serviceName, pair) = await service.TranslateSubtitleLine(
            new TranslateAbleSubtitleLine { SubtitleLine = "hi", SourceLanguage = "en", TargetLanguage = "zh-TW" },
            CancellationToken.None);

        // Assert
        Assert.Equal("fallback", translation);
        Assert.Equal("fallback", serviceName);
        Assert.Equal(MatchTier.Exact, pair.Tier);
        Assert.Equal("zh-TW", pair.Target);
    }

    [Fact]
    public async Task TranslateSubtitleLine_SameTierBreaksTiesByChainOrder()
    {
        // Arrange
        var primary = MockService(new LanguagePair { Source = "en", Target = "zh-TW", Tier = MatchTier.Exact }, _ => "primary");
        var fallback = MockService(new LanguagePair { Source = "en", Target = "zh-TW", Tier = MatchTier.Exact }, _ => "fallback");
        var service = new SubtitleTranslationService(
            [
                new TranslationServiceEntry("primary", primary.Object, null),
                new TranslationServiceEntry("fallback", fallback.Object, null)
            ],
            NullLogger.Instance);

        // Act
        var (translation, serviceName, _) = await service.TranslateSubtitleLine(
            new TranslateAbleSubtitleLine { SubtitleLine = "hi", SourceLanguage = "en", TargetLanguage = "zh-TW" },
            CancellationToken.None);

        // Assert
        Assert.Equal("primary", translation);
        Assert.Equal("primary", serviceName);
    }

    [Fact]
    public async Task TranslateSubtitleLine_FallbackMatchSurfacesMatchedCodesOnEmittedLine()
    {
        // Arrange
        var only = MockService(new LanguagePair { Source = "en", Target = "zh", Tier = MatchTier.NeutralEquivalent }, _ => "ni hao");
        var progress = new Mock<IProgressService>();
        progress.Setup(progressService => progressService.Emit(It.IsAny<TranslationRequest>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        progress.Setup(progressService => progressService.EmitLine(
                It.IsAny<TranslationRequest>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<LanguagePair?>()))
            .Returns(Task.CompletedTask);
        var service = new SubtitleTranslationService(
            [new TranslationServiceEntry("only", only.Object, null)],
            NullLogger.Instance,
            progress.Object);
        var request = new TranslationRequest
        {
            Title = "t",
            SourceLanguage = "en",
            TargetLanguage = "zh-TW",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.InProgress
        };

        // Act
        await service.TranslateSubtitles([Subtitle(1, "hi")], request,
            stripSubtitleFormatting: false,
            preserveLineBreaks: false,
            contextBefore: 0,
            contextAfter: 0,
            CancellationToken.None);

        // Assert
        progress.Verify(progressService => progressService.EmitLine(
            request, 1, "hi", "ni hao", "only",
            It.Is<LanguagePair?>(pair => pair != null && pair.Source == "en" && pair.Target == "zh" && pair.Tier == MatchTier.NeutralEquivalent)),
            Times.Once);
    }

    [Fact]
    public async Task TranslateSubtitleLine_NoServiceSupportsPair_Throws()
    {
        // Arrange
        var none = MockService(pair: null, _ => "unused");
        var service = new SubtitleTranslationService(
            [new TranslationServiceEntry("none", none.Object, null)],
            NullLogger.Instance);

        // Act + Assert
        await Assert.ThrowsAsync<TranslationException>(() =>
            service.TranslateSubtitleLine(
                new TranslateAbleSubtitleLine { SubtitleLine = "hi", SourceLanguage = "en", TargetLanguage = "zh-TW" },
                CancellationToken.None));
    }

    #endregion
}
