using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Models.Batch;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Controllers;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Batch.Response;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Language.Flow;
using Xunit;

namespace Lingarr.Server.Tests.Controllers;

public class TranslateControllerContentTests : IDisposable
{
    private const int RadarrMovieId = 77;

    private readonly SqliteConnection _connection;
    private readonly LingarrDbContext _context;
    private readonly Mock<ITranslationService> _translationServiceMock;
    private readonly Mock<IBatchTranslationService> _batchTranslationServiceMock;
    private readonly Mock<ITranslationServiceFactory> _translationServiceFactoryMock;
    private readonly Mock<IProgressService> _progressServiceMock;
    private readonly Mock<IStatisticsService> _statisticsServiceMock;
    private readonly Mock<ISettingService> _settingServiceMock;
    private readonly Dictionary<string, string> _settings;
    private readonly TranslationRequestService _translationRequestService;
    private readonly TranslateController _controller;
    private readonly Func<IProgressService, TranslationRequestService> _createTranslationRequestService;
    private readonly ProgressService _progressService;
    private int _movieId;

    public TranslateControllerContentTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LingarrDbContext(options);
        _context.Database.EnsureCreated();

        _context.Movies.Add(new Movie
        {
            RadarrId = RadarrMovieId,
            Title = "Canonical Movie",
            FileName = null,
            Path = null,
            DateAdded = DateTime.UtcNow,
            IncludeInTranslation = true
        });
        _context.SaveChanges();
        _movieId = _context.Movies.First().Id;

        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock
            .Setup(translationService => translationService.ModelName)
            .Returns("test-model");
        _translationServiceMock
            .Setup(translationService => translationService.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string source, string target, CancellationToken _) =>
                new LanguagePair { Source = source, Target = target, Tier = MatchTier.Exact });
        _translationServiceMock
            .Setup(translationService => translationService.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) =>
                $"line:{text}");

        // The content endpoint gates the batch path on the translation service itself
        // implementing IBatchTranslationService, so one mock plays both roles.
        _batchTranslationServiceMock = _translationServiceMock.As<IBatchTranslationService>();
        _batchTranslationServiceMock
            .Setup(batchService => batchService.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<BatchSubtitleItem> items, string _, string _, CancellationToken _) =>
                items.ToDictionary(item => item.Position, item => $"batch:{item.Line}"));

        _translationServiceFactoryMock = new Mock<ITranslationServiceFactory>();
        _translationServiceFactoryMock
            .Setup(factory => factory.CreateTranslationServices(It.IsAny<IReadOnlyList<string>>()))
            .Returns((IReadOnlyList<string> serviceTypes) => serviceTypes
                .Select(serviceType => new TranslationServiceEntry(
                    serviceType,
                    _translationServiceMock.Object,
                    (IBatchTranslationService)_translationServiceMock.Object))
                .ToList());

        _settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.UseBatchTranslation, "true" },
            { SettingKeys.Translation.ServiceType, "[\"openai\"]" },
            { SettingKeys.Translation.MaxBatchSize, "10000" },
            { SettingKeys.Translation.StripSubtitleFormatting, "false" },
            { SettingKeys.Translation.PreserveLineBreaks, "false" },
            { SettingKeys.Translation.AiContextBefore, "0" },
            { SettingKeys.Translation.AiContextAfter, "0" },
            { SettingKeys.Translation.AiContextUseTranslated, "false" }
        };
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock
            .Setup(settingService => settingService.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(() => new Dictionary<string, string>(_settings));
        _settingServiceMock
            .Setup(settingService => settingService.GetSetting(SettingKeys.Translation.ServiceType))
            .ReturnsAsync(() => _settings[SettingKeys.Translation.ServiceType]);

        _progressServiceMock = new Mock<IProgressService>();
        _progressServiceMock
            .Setup(progressService => progressService.Emit(It.IsAny<TranslationRequest>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _progressServiceMock
            .Setup(progressService => progressService.EmitLine(
                It.IsAny<TranslationRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<LanguagePair?>()))
            .Returns(Task.CompletedTask);
        _progressServiceMock
            .Setup(progressService => progressService.EmitLines(
                It.IsAny<TranslationRequest>(),
                It.IsAny<List<TranslatedLineData>>()))
            .Returns(Task.CompletedTask);

        _statisticsServiceMock = new Mock<IStatisticsService>();
        _statisticsServiceMock
            .Setup(statisticsService => statisticsService.UpdateTranslationStatisticsFromLines(
                It.IsAny<TranslationRequest>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<BatchTranslatedLine[]>()))
            .ReturnsAsync(0);

        var mediaServiceMock = new Mock<IMediaService>();
        mediaServiceMock
            .Setup(mediaService => mediaService.GetMovieIdOrSyncFromRadarrMovieId(RadarrMovieId))
            .ReturnsAsync(() => _movieId);

        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(clientProxy => clientProxy.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock
            .Setup(hubClients => hubClients.Group(It.IsAny<string>()))
            .Returns(clientProxyMock.Object);
        var hubContextMock = new Mock<IHubContext<TranslationRequestsHub>>();
        hubContextMock
            .Setup(hubContext => hubContext.Clients)
            .Returns(hubClientsMock.Object);

        _progressService = new ProgressService(hubContextMock.Object, _context);
        _createTranslationRequestService = progressService => new TranslationRequestService(
            _context,
            new Mock<IBackgroundJobClient>().Object,
            hubContextMock.Object,
            _translationServiceFactoryMock.Object,
            progressService,
            _statisticsServiceMock.Object,
            mediaServiceMock.Object,
            _settingServiceMock.Object,
            new Mock<ISubtitleService>().Object,
            new Mock<ITranslationRequestEventService>().Object,
            NullLogger<TranslationRequestService>.Instance);

        _translationRequestService = _createTranslationRequestService(_progressServiceMock.Object);
        _controller = new TranslateController(
            _translationServiceFactoryMock.Object,
            _translationRequestService,
            _settingServiceMock.Object,
            new LanguageCodeService(),
            NullLogger<TranslateController>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static TranslateAbleSubtitleContent NewContent(params string[] lines) => new()
    {
        ArrMediaId = RadarrMovieId,
        SourceLanguage = "en",
        TargetLanguage = "es",
        MediaType = MediaType.Movie,
        Lines = lines
            .Select((line, index) => new BatchSubtitleLine { Position = index, Line = line })
            .ToList()
    };

    private static BatchTranslatedLine[] AssertOk(ActionResult<BatchTranslatedLine[]> response)
    {
        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        return Assert.IsType<BatchTranslatedLine[]>(okResult.Value);
    }

    private TranslationRequest SeedRequest(TranslationStatus status, string target = "stored")
    {
        var request = new TranslationRequest
        {
            MediaId = _movieId,
            MediaType = MediaType.Movie,
            Title = "Canonical Movie",
            SourceLanguage = "en",
            TargetLanguage = "es",
            Status = status,
            JobType = TranslationJobType.Translation
        };
        _context.TranslationRequests.Add(request);
        _context.TranslationRequestLines.AddRange(NewContent("First line", "Second line").Lines
            .Select(line => new TranslationRequestLine
            {
                TranslationRequest = request,
                Position = line.Position,
                Source = line.Line,
                Target = $"{target} {line.Position}"
            }));
        _context.SaveChanges();
        return request;
    }

    private ISetup<ITranslationService, Task<string>> SetupLineTranslation() => _translationServiceMock
        .Setup(service => service.TranslateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()));

    private void VerifyNoLineCall() => _translationServiceMock.Verify(
        service => service.TranslateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()),
        Times.Never);

    private void VerifyNoBatchCall() => _batchTranslationServiceMock.Verify(
        batchService => batchService.TranslateBatchAsync(
            It.IsAny<List<BatchSubtitleItem>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
        Times.Never);

    [Fact]
    public async Task TranslateContent_BatchEnabled_TranslatesEveryLineInOneCall()
    {
        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line", "Third line"));

        var results = AssertOk(response);
        Assert.Equal(
            ["batch:First line", "batch:Second line", "batch:Third line"],
            results.OrderBy(result => result.Position).Select(result => result.Line));
        _batchTranslationServiceMock.Verify(batchService => batchService.TranslateBatchAsync(
            It.IsAny<List<BatchSubtitleItem>>(),
            "en",
            "es",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateContent_BatchEnabled_MarksRequestCompleted()
    {
        await _controller.TranslateContent(NewContent("First line", "Second line"));

        var translationRequest = await _context.TranslationRequests.SingleAsync();
        Assert.Equal(TranslationStatus.Completed, translationRequest.Status);
        Assert.Equal("Canonical Movie", translationRequest.Title);
        Assert.Equal(_movieId, translationRequest.MediaId);
        Assert.NotNull(translationRequest.CompletedAt);
        _statisticsServiceMock.Verify(statisticsService => statisticsService.UpdateTranslationStatisticsFromLines(
            It.IsAny<TranslationRequest>(),
            "openai",
            "test-model",
            It.Is<BatchTranslatedLine[]>(lines => lines.Length == 2)), Times.Once);
        _progressServiceMock.Verify(
            progressService => progressService.Emit(It.IsAny<TranslationRequest>(), 100),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TranslateContent_MaxBatchSizeSmallerThanLineCount_SplitsIntoChunks()
    {
        _settings[SettingKeys.Translation.MaxBatchSize] = "2";

        var response = await _controller.TranslateContent(
            NewContent("One", "Two", "Three", "Four", "Five"));

        var results = AssertOk(response);
        Assert.Equal(5, results.Length);
        _batchTranslationServiceMock.Verify(batchService => batchService.TranslateBatchAsync(
            It.IsAny<List<BatchSubtitleItem>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        _progressServiceMock.Verify(progressService => progressService.EmitLines(
            It.IsAny<TranslationRequest>(),
            It.IsAny<List<TranslatedLineData>>()), Times.Exactly(3));
    }

    [Fact]
    public async Task TranslateContent_MaxBatchSizeNotANumber_TranslatesEveryLineInOneCall()
    {
        _settings[SettingKeys.Translation.MaxBatchSize] = "not a number";

        var response = await _controller.TranslateContent(
            NewContent("One", "Two", "Three"));

        Assert.Equal(3, AssertOk(response).Length);
        _batchTranslationServiceMock.Verify(batchService => batchService.TranslateBatchAsync(
            It.IsAny<List<BatchSubtitleItem>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateContent_MalformedServiceTypeSetting_UsesDefaultService()
    {
        _settings[SettingKeys.Translation.ServiceType] = "[\"openai\"";

        await _controller.TranslateContent(NewContent("First line", "Second line"));

        _translationServiceFactoryMock.Verify(
            factory => factory.CreateTranslationServices(It.Is<IReadOnlyList<string>>(serviceTypes =>
                serviceTypes.Count == 1 && serviceTypes[0] == SettingKeys.Translation.DefaultServiceType)),
            Times.Once);
    }

    [Fact]
    public async Task TranslateContent_StripFormattingOn_RemovesMarkupFromTranslatedLine()
    {
        _settings[SettingKeys.Translation.StripSubtitleFormatting] = "true";

        var response = await _controller.TranslateContent(
            NewContent("<i>First line</i>", "{\\an8}Second line"));

        var results = AssertOk(response);
        Assert.Equal(
            ["batch:First line", "batch:Second line"],
            results.OrderBy(result => result.Position).Select(result => result.Line));
        _batchTranslationServiceMock.Verify(batchService => batchService.TranslateBatchAsync(
            It.Is<List<BatchSubtitleItem>>(items => items[0].Line == "<i>First line</i>"),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateContent_StripFormattingOff_KeepsMarkupInTranslatedLine()
    {
        var response = await _controller.TranslateContent(
            NewContent("<i>First line</i>", "Second line"));

        var results = AssertOk(response);
        Assert.Equal("batch:<i>First line</i>", results.Single(result => result.Position == 0).Line);
    }

    [Fact]
    public async Task TranslateContent_StripFormattingOnLineByLine_RemovesMarkupFromTranslatedLine()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";
        _settings[SettingKeys.Translation.StripSubtitleFormatting] = "true";

        var response = await _controller.TranslateContent(
            NewContent("<i>First line</i>", "{\\an8}Second line"));

        var results = AssertOk(response);
        Assert.Equal(
            ["line:First line", "line:Second line"],
            results.OrderBy(result => result.Position).Select(result => result.Line));
        _translationServiceMock.Verify(translationService => translationService.TranslateAsync(
            "<i>First line</i>",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>?>(),
            It.IsAny<List<string>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateContent_StripFormattingOffLineByLine_KeepsMarkupInTranslatedLine()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";

        var response = await _controller.TranslateContent(
            NewContent("<i>First line</i>", "Second line"));

        var results = AssertOk(response);
        Assert.Equal("line:<i>First line</i>", results.Single(result => result.Position == 0).Line);
    }

    [Fact]
    public async Task TranslateContent_PreserveLineBreaksOn_KeepsProviderOutputVerbatim()
    {
        _settings[SettingKeys.Translation.PreserveLineBreaks] = "true";
        _batchTranslationServiceMock
            .Setup(batchService => batchService.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<BatchSubtitleItem> items, string _, string _, CancellationToken _) =>
                items.ToDictionary(item => item.Position, item => $"batch:{item.Line}"));

        var response = await _controller.TranslateContent(
            NewContent("First\nline", "Second line"));

        var results = AssertOk(response);
        Assert.Equal("batch:First\nline", results.Single(result => result.Position == 0).Line);
    }

    [Fact]
    public async Task TranslateContent_PrimaryServiceFails_FallsBackToNextServiceButReportsPrimary()
    {
        _settings[SettingKeys.Translation.ServiceType] = "[\"openai\",\"deepl\"]";

        var failingServiceMock = new Mock<ITranslationService>();
        failingServiceMock
            .Setup(translationService => translationService.ModelName)
            .Returns("failing-model");
        failingServiceMock
            .Setup(translationService => translationService.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string source, string target, CancellationToken _) =>
                new LanguagePair { Source = source, Target = target, Tier = MatchTier.Exact });
        failingServiceMock.As<IBatchTranslationService>()
            .Setup(batchService => batchService.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider is down"));

        _translationServiceFactoryMock
            .Setup(factory => factory.CreateTranslationServices(It.IsAny<IReadOnlyList<string>>()))
            .Returns((IReadOnlyList<string> serviceTypes) => new List<TranslationServiceEntry>
            {
                new(serviceTypes[0], failingServiceMock.Object, (IBatchTranslationService)failingServiceMock.Object),
                new(serviceTypes[1], _translationServiceMock.Object, (IBatchTranslationService)_translationServiceMock.Object)
            });

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        var results = AssertOk(response);
        Assert.Equal(
            ["batch:First line", "batch:Second line"],
            results.OrderBy(result => result.Position).Select(result => result.Line));

        // Statistics are keyed off the first configured service, not the one that did the work.
        _statisticsServiceMock.Verify(statisticsService => statisticsService.UpdateTranslationStatisticsFromLines(
            It.IsAny<TranslationRequest>(),
            "openai",
            "failing-model",
            It.IsAny<BatchTranslatedLine[]>()), Times.Once);
    }

    [Fact]
    public async Task TranslateContent_BatchDisabled_FallsBackToLineByLine()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        var results = AssertOk(response);
        Assert.Equal(
            ["line:First line", "line:Second line"],
            results.OrderBy(result => result.Position).Select(result => result.Line));
        VerifyNoBatchCall();
    }

    [Fact]
    public async Task TranslateContent_SingleLine_FallsBackToLineByLine()
    {
        var response = await _controller.TranslateContent(NewContent("Only line"));

        var results = AssertOk(response);
        Assert.Equal("line:Only line", Assert.Single(results).Line);
        VerifyNoBatchCall();
    }

    [Fact]
    public async Task TranslateContent_ServiceWithoutBatchSupport_FallsBackToLineByLine()
    {
        var plainServiceMock = new Mock<ITranslationService>();
        plainServiceMock
            .Setup(translationService => translationService.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string source, string target, CancellationToken _) =>
                new LanguagePair { Source = source, Target = target, Tier = MatchTier.Exact });
        plainServiceMock
            .Setup(translationService => translationService.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) =>
                $"line:{text}");
        _translationServiceFactoryMock
            .Setup(factory => factory.CreateTranslationServices(It.IsAny<IReadOnlyList<string>>()))
            .Returns((IReadOnlyList<string> serviceTypes) => serviceTypes
                .Select(serviceType => new TranslationServiceEntry(serviceType, plainServiceMock.Object, null))
                .ToList());

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        var results = AssertOk(response);
        Assert.Equal(
            ["line:First line", "line:Second line"],
            results.OrderBy(result => result.Position).Select(result => result.Line));
        VerifyNoBatchCall();
    }

    [Fact]
    public async Task TranslateContent_LineByLineWithBlankLine_PassesBlankThroughUntranslated()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";

        var response = await _controller.TranslateContent(
            NewContent("First line", "   "));

        var results = AssertOk(response);
        Assert.Equal("   ", results.Single(result => result.Position == 1).Line);
        _translationServiceMock.Verify(translationService => translationService.TranslateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>?>(),
            It.IsAny<List<string>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateContent_TitleOverride_IsUsedInsteadOfCanonicalTitle()
    {
        var content = NewContent("First line", "Second line");
        content.Title = "  Client Title  ";

        await _controller.TranslateContent(content);

        var translationRequest = await _context.TranslationRequests.SingleAsync();
        Assert.Equal("Client Title", translationRequest.Title);
    }

    [Fact]
    public async Task TranslateContent_DuplicateActiveRequest_ReturnsConflictWithoutTranslating()
    {
        _context.TranslationRequests.Add(new TranslationRequest
        {
            MediaId = _movieId,
            MediaType = MediaType.Movie,
            Title = "Canonical Movie",
            SourceLanguage = "en",
            TargetLanguage = "es",
            Status = TranslationStatus.InProgress,
            JobType = TranslationJobType.Translation
        });
        await _context.SaveChangesAsync();

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ConflictObjectResult>(response.Result).StatusCode);
        VerifyNoLineCall();
        VerifyNoBatchCall();
        Assert.Equal(1, await _context.TranslationRequests.CountAsync());
    }

    [Fact]
    public async Task TranslateContent_CompletedMatchingLines_ReturnsNewestStoredLinesInIncomingOrder()
    {
        SeedRequest(TranslationStatus.Completed, "older");
        SeedRequest(TranslationStatus.Completed);
        var content = NewContent("First line", "Second line");
        content.Lines.Reverse();

        var results = AssertOk(await _controller.TranslateContent(content));

        Assert.Equal(new[] { 1, 0 }, results.Select(line => line.Position));
        Assert.Equal(new[] { "stored 1", "stored 0" }, results.Select(line => line.Line));
        Assert.Equal(2, await _context.TranslationRequests.CountAsync());
        VerifyNoLineCall();
        VerifyNoBatchCall();
    }

    [Fact]
    public async Task TranslateContent_CompletedMatchingMarkup_StripsFormatting()
    {
        _settings[SettingKeys.Translation.StripSubtitleFormatting] = "true";
        SeedRequest(TranslationStatus.Completed, "<i>stored</i>");

        var results = AssertOk(await _controller.TranslateContent(NewContent("First line", "Second line")));

        Assert.Equal(new[] { "stored 0", "stored 1" }, results.Select(line => line.Line));
        VerifyNoLineCall();
        VerifyNoBatchCall();
    }

    public static TheoryData<List<BatchSubtitleLine>> MismatchedLines => new()
    {
        new List<BatchSubtitleLine>
        {
            new() { Position = 0, Line = "changed" },
            new() { Position = 1, Line = "Second line" }
        },
        new List<BatchSubtitleLine> { new() { Position = 0, Line = "First line" } },
        new List<BatchSubtitleLine>
        {
            new() { Position = 2, Line = "First line" },
            new() { Position = 1, Line = "Second line" }
        },
        new List<BatchSubtitleLine>
        {
            new() { Position = 0, Line = "First line" },
            new() { Position = 0, Line = "First line" }
        }
    };

    [Theory]
    [MemberData(nameof(MismatchedLines))]
    public async Task TranslateContent_CompletedMismatchedLines_TranslatesFresh(List<BatchSubtitleLine> lines)
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";
        SeedRequest(TranslationStatus.Completed);
        var content = NewContent();
        content.Lines = lines;

        var results = AssertOk(await _controller.TranslateContent(content));

        Assert.Equal(lines.Count, results.Length);
        Assert.All(results, line => Assert.StartsWith("line:", line.Line));
        Assert.Equal(2, await _context.TranslationRequests.CountAsync());
        Assert.Equal(TranslationStatus.Completed,
            (await _context.TranslationRequests.OrderByDescending(request => request.Id).FirstAsync()).Status);
    }

    [Theory]
    [InlineData(TranslationStatus.Pending)]
    [InlineData(TranslationStatus.InProgress)]
    public async Task TranslateContent_ActiveDuplicateWithCompletedRequest_ReturnsConflict(TranslationStatus status)
    {
        SeedRequest(TranslationStatus.Completed);
        SeedRequest(status);

        var response = await _controller.TranslateContent(NewContent("First line", "Second line"));

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ConflictObjectResult>(response.Result).StatusCode);
        Assert.Equal(2, await _context.TranslationRequests.CountAsync());
        VerifyNoLineCall();
        VerifyNoBatchCall();
    }

    [Theory]
    [InlineData(TranslationStatus.Failed)]
    [InlineData(TranslationStatus.Interrupted)]
    [InlineData(TranslationStatus.Cancelled)]
    public async Task TranslateContent_NewestRequestNotCompleted_TranslatesFresh(TranslationStatus status)
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";
        SeedRequest(TranslationStatus.Completed);
        SeedRequest(status);

        var results = AssertOk(await _controller.TranslateContent(NewContent("First line", "Second line")));

        Assert.Equal(new[] { "line:First line", "line:Second line" }, results.Select(line => line.Line));
        Assert.Equal(3, await _context.TranslationRequests.CountAsync());
    }

    [Fact]
    public async Task TranslateContent_ClientDisconnectsDuringLineTranslation_Completes()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";
        using var parent = new CancellationTokenSource();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestAborted = parent.Token }
        };
        SetupLineTranslation()
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken token) =>
            {
                parent.Cancel();
                Assert.Equal(TranslationStatus.InProgress, _context.TranslationRequests.Single().Status);
                token.ThrowIfCancellationRequested();
                return $"line:{text}";
            });

        var results = AssertOk(await _controller.TranslateContent(NewContent("First line", "Second line")));

        Assert.Equal(new[] { "line:First line", "line:Second line" }, results.Select(line => line.Line));
        var request = await _context.TranslationRequests.SingleAsync();
        Assert.Equal(TranslationStatus.Completed, request.Status);
        Assert.Null(request.ErrorMessage);
    }

    [Fact]
    public async Task TranslateContent_ApiCancelDuringTranslation_Cancels()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";
        SetupLineTranslation()
            .Returns(async (string text, string _, string _, List<string>? _, List<string>? _, CancellationToken token) =>
            {
                var request = await _context.TranslationRequests.SingleAsync();
                await _translationRequestService.CancelTranslationRequest(request);
                token.ThrowIfCancellationRequested();
                return text;
            });

        var response = await _controller.TranslateContent(NewContent("First line", "Second line"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(response.Result).StatusCode);
        Assert.Equal(TranslationStatus.Cancelled, (await _context.TranslationRequests.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TranslateContent_CompletedTranslation_WithRealProgress_ReusesStrippedResult(bool useBatch)
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = useBatch ? "true" : "false";
        _settings[SettingKeys.Translation.StripSubtitleFormatting] = "true";
        SetupLineTranslation()
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) =>
                $"<i>line:{text}</i>");
        _batchTranslationServiceMock
            .Setup(service => service.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<BatchSubtitleItem> items, string _, string _, CancellationToken _) =>
                items.ToDictionary(item => item.Position, item => $"<i>line:{item.Line}</i>"));
        var controller = new TranslateController(
            _translationServiceFactoryMock.Object,
            _createTranslationRequestService(_progressService),
            _settingServiceMock.Object,
            new LanguageCodeService(),
            NullLogger<TranslateController>.Instance);
        var content = NewContent("First line", "Second line");
        content.Lines.Reverse();

        var first = AssertOk(await controller.TranslateContent(content));
        Assert.Equal(TranslationStatus.Completed, (await _context.TranslationRequests.SingleAsync()).Status);
        Assert.All(await _context.TranslationRequestLines.ToListAsync(), line =>
            Assert.Equal(useBatch ? $"line:{line.Source}" : $"<i>line:{line.Source}</i>", line.Target));
        _translationServiceMock.Invocations.Clear();
        var second = AssertOk(await controller.TranslateContent(content));

        Assert.Equal(new[] { "line:Second line", "line:First line" }, first.Select(line => line.Line));
        Assert.Equal(first.Select(line => (line.Position, line.Line)), second.Select(line => (line.Position, line.Line)));
        Assert.Equal(1, await _context.TranslationRequests.CountAsync());
        VerifyNoLineCall();
        VerifyNoBatchCall();
    }

    [Fact]
    public async Task TranslateContent_CompletedDuplicatePositions_WithRealProgress_TranslatesFresh()
    {
        _settings[SettingKeys.Translation.UseBatchTranslation] = "false";
        var controller = new TranslateController(
            _translationServiceFactoryMock.Object,
            _createTranslationRequestService(_progressService),
            _settingServiceMock.Object,
            new LanguageCodeService(),
            NullLogger<TranslateController>.Instance);
        var content = NewContent("First line", "First line");
        content.Lines[1].Position = 0;

        var first = AssertOk(await controller.TranslateContent(content));
        Assert.Equal(TranslationStatus.Completed, (await _context.TranslationRequests.SingleAsync()).Status);
        Assert.Equal(2, await _context.TranslationRequestLines.CountAsync());
        _translationServiceMock.Invocations.Clear();
        var second = AssertOk(await controller.TranslateContent(content));

        Assert.Equal(new[] { "line:First line", "line:First line" }, first.Select(line => line.Line));
        Assert.Equal(first.Select(line => (line.Position, line.Line)), second.Select(line => (line.Position, line.Line)));
        Assert.Equal(2, await _context.TranslationRequests.CountAsync());
        Assert.All(await _context.TranslationRequests.ToListAsync(), request =>
            Assert.Equal(TranslationStatus.Completed, request.Status));
        _translationServiceMock.Verify(service => service.TranslateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        VerifyNoBatchCall();
    }

    [Fact]
    public async Task TranslateContent_NoUsableServices_ReturnsServerError()
    {
        _translationServiceFactoryMock
            .Setup(factory => factory.CreateTranslationServices(It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<TranslationServiceEntry>());

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        var errorResult = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(500, errorResult.StatusCode);
        Assert.Empty(await _context.TranslationRequests.ToListAsync());
    }

    [Fact]
    public async Task TranslateContent_BatchServiceFails_ReturnsServerErrorAndMarksRequestFailed()
    {
        _batchTranslationServiceMock
            .Setup(batchService => batchService.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider is down"));

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        var errorResult = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(500, errorResult.StatusCode);

        var translationRequest = await _context.TranslationRequests.SingleAsync();
        Assert.Equal(TranslationStatus.Failed, translationRequest.Status);
        Assert.NotNull(translationRequest.ErrorMessage);
        Assert.NotNull(translationRequest.CompletedAt);
    }

    [Fact]
    public async Task TranslateContent_ClientDisconnectsDuringBatchTranslation_ReturnsResultsAndCompletes()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestAborted = cancellationTokenSource.Token }
        };
        _batchTranslationServiceMock
            .Setup(batchService => batchService.TranslateBatchAsync(
                It.IsAny<List<BatchSubtitleItem>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (List<BatchSubtitleItem> items, string _, string _, CancellationToken token) =>
            {
                await cancellationTokenSource.CancelAsync();
                Assert.Equal(TranslationStatus.InProgress, _context.TranslationRequests.Single().Status);
                token.ThrowIfCancellationRequested();
                return items.ToDictionary(item => item.Position, item => $"batch:{item.Line}");
            });

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        Assert.Equal(new[] { "batch:First line", "batch:Second line" }, AssertOk(response).Select(line => line.Line));

        var translationRequest = await _context.TranslationRequests.SingleAsync();
        Assert.Equal(TranslationStatus.Completed, translationRequest.Status);
        Assert.NotNull(translationRequest.CompletedAt);
    }

    [Fact]
    public async Task TranslateContent_UnsupportedLanguagePair_ReturnsServerErrorAndMarksRequestFailed()
    {
        _translationServiceMock
            .Setup(translationService => translationService.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LanguagePair?)null);

        var response = await _controller.TranslateContent(
            NewContent("First line", "Second line"));

        var errorResult = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(500, errorResult.StatusCode);

        var translationRequest = await _context.TranslationRequests.SingleAsync();
        Assert.Equal(TranslationStatus.Failed, translationRequest.Status);
    }
}
