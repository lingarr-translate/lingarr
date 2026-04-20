using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GTranslate;
using GTranslate.Results;
using GTranslate.Translators;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class TranslationTextChunkerTests
{
    [Fact]
    public void Split_ShouldReturnOriginalText_WhenWithinLimit()
    {
        var text = "Short subtitle line";

        var chunks = TranslationTextChunker.Split(text, 1000);

        Assert.Single(chunks);
        Assert.Equal(text, chunks[0]);
    }

    [Fact]
    public void Split_ShouldPreferWhitespaceBoundary_WhenTextExceedsLimit()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 260));

        var chunks = TranslationTextChunker.Split(text, 1000);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= 1000));
        Assert.Equal(text, string.Join(" ", chunks));
    }

    [Fact]
    public void Split_ShouldFallbackToHardCut_WhenNoWhitespaceExists()
    {
        var text = new string('a', 2100);

        var chunks = TranslationTextChunker.Split(text, 1000);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= 1000));
        Assert.Equal(text, string.Concat(chunks));
    }

    [Fact]
    public async Task GTranslatorService_ShouldSplitMicrosoftInput_AndRecombineTranslatedChunks()
    {
        var settings = new Dictionary<string, string>
        {
            { SettingKeys.Translation.MaxRetries, "0" },
            { SettingKeys.Translation.RetryDelay, "0" },
            { SettingKeys.Translation.RetryDelayMultiplier, "2" }
        };

        var settingsMock = new Mock<ISettingService>();
        settingsMock.Setup(service => service.GetSettings(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(settings);

        var translator = new MicrosoftTranslator();
        var service = new GTranslatorService<MicrosoftTranslator>(
            translator,
            "/tmp/microsoft_languages.json",
            settingsMock.Object,
            NullLogger.Instance,
            new LanguageCodeService(NullLogger<LanguageCodeService>.Instance));

        var input = string.Join(" ", Enumerable.Repeat("word", 260));
        var expectedChunks = TranslationTextChunker.Split(input, 1000);

        var result = await service.TranslateAsync(
            input,
            "en",
            "es",
            null,
            null,
            CancellationToken.None);

        Assert.Equal(input, result);
        Assert.Equal(expectedChunks.Count, translator.ReceivedTexts.Count);
        Assert.All(translator.ReceivedTexts, chunk => Assert.True(chunk.Length <= 1000));
    }

    private sealed class MicrosoftTranslator : ITranslator
    {
        public List<string> ReceivedTexts { get; } = [];

        public string Name => nameof(MicrosoftTranslator);

        public Task<ITranslationResult> TranslateAsync(string text, string toLanguage, string? fromLanguage = null)
        {
            ReceivedTexts.Add(text);
            return Task.FromResult<ITranslationResult>(CreateResult(text, toLanguage, fromLanguage ?? "en"));
        }

        public Task<ITranslationResult> TranslateAsync(string text, ILanguage toLanguage, ILanguage? fromLanguage = null)
        {
            ReceivedTexts.Add(text);
            return Task.FromResult<ITranslationResult>(CreateResult(text, toLanguage.ISO6391, fromLanguage?.ISO6391 ?? "en"));
        }

        public Task<ITransliterationResult> TransliterateAsync(string text, string toLanguage, string? fromLanguage = null)
            => throw new NotImplementedException();

        public Task<ITransliterationResult> TransliterateAsync(string text, ILanguage toLanguage, ILanguage? fromLanguage = null)
            => throw new NotImplementedException();

        public Task<ILanguage> DetectLanguageAsync(string text)
            => throw new NotImplementedException();

        public bool IsLanguageSupported(string language) => true;

        public bool IsLanguageSupported(ILanguage language) => true;

        private static ITranslationResult CreateResult(string text, string toLanguage, string fromLanguage)
        {
            var targetLanguage = Language.GetLanguage(toLanguage);
            var sourceLanguage = Language.GetLanguage(fromLanguage);

            var result = (MicrosoftTranslationResult)Activator.CreateInstance(
                typeof(MicrosoftTranslationResult),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: [text, text, targetLanguage, sourceLanguage, 1f],
                culture: null)!;

            return result;
        }
    }
}
