using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Configuration;
using Lingarr.Core.Enum;
using Lingarr.Server.Controllers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Controllers;

public class TranslateControllerLineTests
{
    private sealed class Harness
    {
        public Mock<ITranslationServiceFactory> TranslationServiceFactoryMock { get; init; } = null!;
        public Mock<ITranslationService> PrimaryServiceMock { get; init; } = null!;
        public Mock<ITranslationService> FallbackServiceMock { get; init; } = null!;
        public TranslateController Controller { get; init; } = null!;
    }

    private static Mock<ITranslationService> CreateTranslationService(
        Func<string, string>? translate = null,
        MatchTier? tier = MatchTier.Exact)
    {
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock
            .Setup(translationService => translationService.GetLanguagePair(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string source, string target, CancellationToken _) => tier is null
                ? null
                : new LanguagePair { Source = source, Target = target, Tier = tier.Value });
        translationServiceMock
            .Setup(translationService => translationService.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) =>
                translate is null ? text : translate(text));
        return translationServiceMock;
    }

    private static Harness CreateHarness(
        string? serviceTypeSetting,
        Mock<ITranslationService>? primaryServiceMock = null)
    {
        primaryServiceMock ??= CreateTranslationService(text => $"translated:{text}");
        var fallbackServiceMock = CreateTranslationService(text => $"fallback:{text}");

        var settingServiceMock = new Mock<ISettingService>();
        settingServiceMock
            .Setup(settingService => settingService.GetSetting(SettingKeys.Translation.ServiceType))
            .ReturnsAsync(serviceTypeSetting);

        var translationServiceFactoryMock = new Mock<ITranslationServiceFactory>();
        translationServiceFactoryMock
            .Setup(factory => factory.CreateTranslationServices(It.IsAny<IReadOnlyList<string>>()))
            .Returns((IReadOnlyList<string> serviceTypes) =>
            {
                var entries = new List<TranslationServiceEntry>
                {
                    new(serviceTypes[0], primaryServiceMock.Object, null)
                };
                for (var index = 1; index < serviceTypes.Count; index++)
                {
                    entries.Add(new TranslationServiceEntry(serviceTypes[index], fallbackServiceMock.Object, null));
                }

                return entries;
            });

        return new Harness
        {
            TranslationServiceFactoryMock = translationServiceFactoryMock,
            PrimaryServiceMock = primaryServiceMock,
            FallbackServiceMock = fallbackServiceMock,
            Controller = new TranslateController(
                translationServiceFactoryMock.Object,
                new Mock<ITranslationRequestService>().Object,
                settingServiceMock.Object,
                new LanguageCodeService(),
                NullLogger<TranslateController>.Instance)
        };
    }

    private static TranslateAbleSubtitleLine NewLine(
        string subtitleLine = "Hello there",
        List<string>? contextLinesBefore = null,
        List<string>? contextLinesAfter = null) => new()
    {
        SubtitleLine = subtitleLine,
        SourceLanguage = "en",
        TargetLanguage = "es",
        ContextLinesBefore = contextLinesBefore,
        ContextLinesAfter = contextLinesAfter
    };

    [Fact]
    public async Task TranslateLine_ReturnsTranslationFromConfiguredService()
    {
        var harness = CreateHarness("openai");

        var result = await harness.Controller.TranslateLine(NewLine(), CancellationToken.None);

        Assert.Equal("translated:Hello there", result);
        harness.PrimaryServiceMock.Verify(translationService => translationService.TranslateAsync(
            "Hello there",
            "en",
            "es",
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateLine_ServiceTypeList_UsesPrimaryServiceOnly()
    {
        var harness = CreateHarness("[\"openai\",\"deepl\"]");

        var result = await harness.Controller.TranslateLine(NewLine(), CancellationToken.None);

        Assert.Equal("translated:Hello there", result);
        harness.TranslationServiceFactoryMock.Verify(
            factory => factory.CreateTranslationServices(It.Is<IReadOnlyList<string>>(serviceTypes =>
                serviceTypes.Count == 1 && serviceTypes[0] == "openai")),
            Times.Once);
        harness.FallbackServiceMock.Verify(translationService => translationService.TranslateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>?>(),
            It.IsAny<List<string>?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TranslateLine_MissingServiceTypeSetting_UsesDefaultService()
    {
        var harness = CreateHarness(null);

        await harness.Controller.TranslateLine(NewLine(), CancellationToken.None);

        harness.TranslationServiceFactoryMock.Verify(
            factory => factory.CreateTranslationServices(It.Is<IReadOnlyList<string>>(serviceTypes =>
                serviceTypes.Count == 1 && serviceTypes[0] == SettingKeys.Translation.DefaultServiceType)),
            Times.Once);
    }

    [Fact]
    public async Task TranslateLine_MalformedServiceTypeSetting_UsesDefaultService()
    {
        var harness = CreateHarness("[\"openai\"");

        await harness.Controller.TranslateLine(NewLine(), CancellationToken.None);

        harness.TranslationServiceFactoryMock.Verify(
            factory => factory.CreateTranslationServices(It.Is<IReadOnlyList<string>>(serviceTypes =>
                serviceTypes.Count == 1 && serviceTypes[0] == SettingKeys.Translation.DefaultServiceType)),
            Times.Once);
    }

    [Fact]
    public async Task TranslateLine_EmptyLine_ReturnsEmptyWithoutTranslating()
    {
        var harness = CreateHarness("openai");

        var result = await harness.Controller.TranslateLine(NewLine(""), CancellationToken.None);

        Assert.Equal("", result);
        harness.PrimaryServiceMock.Verify(translationService => translationService.TranslateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<string>?>(),
            It.IsAny<List<string>?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TranslateLine_PassesContextLinesToTranslationService()
    {
        var harness = CreateHarness("openai");
        var contextLinesBefore = new List<string> { "Previous line" };
        var contextLinesAfter = new List<string> { "Next line" };

        await harness.Controller.TranslateLine(
            NewLine(contextLinesBefore: contextLinesBefore, contextLinesAfter: contextLinesAfter),
            CancellationToken.None);

        harness.PrimaryServiceMock.Verify(translationService => translationService.TranslateAsync(
            "Hello there",
            "en",
            "es",
            contextLinesBefore,
            contextLinesAfter,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateLine_ServiceDoesNotSupportLanguagePair_Throws()
    {
        var harness = CreateHarness("openai", CreateTranslationService(tier: null));

        await Assert.ThrowsAsync<TranslationException>(() =>
            harness.Controller.TranslateLine(NewLine(), CancellationToken.None));
    }

    [Fact]
    public async Task TranslateLine_ServiceFails_Throws()
    {
        var failingServiceMock = CreateTranslationService();
        failingServiceMock
            .Setup(translationService => translationService.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider is down"));
        var harness = CreateHarness("openai", failingServiceMock);

        await Assert.ThrowsAsync<TranslationException>(() =>
            harness.Controller.TranslateLine(NewLine(), CancellationToken.None));
    }

    [Fact]
    public async Task TranslateLine_CancelledToken_Throws()
    {
        var harness = CreateHarness("openai");
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Controller.TranslateLine(NewLine(), cancellationTokenSource.Token));
    }
}
