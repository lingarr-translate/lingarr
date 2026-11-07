using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.Extraction;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Subtitle;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

public class SubtitleExtractionServiceTests : IDisposable
{
    private const string FfmpegPath = "/ffmpeg/ffmpeg";
    private const string FfprobePath = "/ffmpeg/ffprobe";
    private const string FileName = "Movie (2020)";

    private readonly string _mediaDirectory;
    private readonly Mock<IProcessRunner> _processRunner = new();
    private readonly SubtitleExtractionService _service;
    private string _probeOutput = """{ "streams": [] }""";
    private int _ffmpegRuns;

    public SubtitleExtractionServiceTests()
    {
        _mediaDirectory = Path.Combine(Path.GetTempPath(), $"lingarr-extraction-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_mediaDirectory);
        File.WriteAllText(Path.Combine(_mediaDirectory, $"{FileName}.mkv"), "");

        _processRunner
            .Setup(runner => runner.Run(FfprobePath, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ProcessResult(0, _probeOutput, ""));

        // Write a file for every "-c:s <codec> <output>" group, like ffmpeg does.
        _processRunner
            .Setup(runner => runner.Run(FfmpegPath, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<string>, CancellationToken>((_, arguments, _) =>
            {
                _ffmpegRuns++;
                for (var index = 0; index < arguments.Count - 2; index++)
                {
                    if (arguments[index] == "-c:s")
                    {
                        File.WriteAllText(arguments[index + 2], "1\n00:00:01,000 --> 00:00:02,000\nHello\n");
                    }
                }
            })
            .ReturnsAsync(new ProcessResult(0, "", ""));

        var ffmpegService = new Mock<IFfmpegService>();
        ffmpegService.Setup(service => service.FfmpegPath).Returns(FfmpegPath);
        ffmpegService.Setup(service => service.FfprobePath).Returns(FfprobePath);

        _service = new SubtitleExtractionService(
            ffmpegService.Object,
            _processRunner.Object,
            new SubtitleStreamLanguageService(new LanguageCodeService()),
            NullLogger<SubtitleExtractionService>.Instance);
    }

    public void Dispose()
    {
        Directory.Delete(_mediaDirectory, recursive: true);
    }

    [Fact]
    public async Task ExtractSubtitles_WithTaggedStream_WritesSubtitleNextToMedia()
    {
        SetStreams(Stream(2, "subrip", language: "eng"));

        var result = await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(["Movie (2020).en.srt"], GetSubtitleFiles());
    }

    [Fact]
    public async Task ExtractSubtitles_WithForcedHearingImpairedAndAssStreams_NamesFilesForLingarr()
    {
        SetStreams(
            Stream(2, "subrip", language: "eng", title: "Forced"),
            Stream(3, "subrip", language: "eng", hearingImpaired: true),
            Stream(4, "ass", language: "jpn"),
            Stream(5, "subrip", language: "ger", forced: true));

        await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.Equal(
            ["Movie (2020).de.forced.srt", "Movie (2020).en.forced.srt", "Movie (2020).en.sdh.srt", "Movie (2020).ja.ass"],
            GetSubtitleFiles());
    }

    [Fact]
    public async Task ExtractSubtitles_WithMp4TextStreams_WritesSrt()
    {
        SetStreams(Stream(2, "mov_text", language: "eng"), Stream(3, "tx3g", language: "spa"));

        await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.Equal(["Movie (2020).en.srt", "Movie (2020).es.srt"], GetSubtitleFiles());
    }

    [Fact]
    public async Task ExtractSubtitles_WithImageStream_DoesNotRunFfmpeg()
    {
        SetStreams(Stream(2, "hdmv_pgs_subtitle", language: "eng"));

        var result = await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.False(result);
        Assert.Empty(GetSubtitleFiles());
        Assert.Equal(0, _ffmpegRuns);
    }

    [Fact]
    public async Task ExtractSubtitles_WhenSubtitleExists_KeepsItAndDoesNotRunFfmpeg()
    {
        var existingPath = Path.Combine(_mediaDirectory, "Movie (2020).en.srt");
        File.WriteAllText(existingPath, "existing");
        SetStreams(Stream(2, "subrip", language: "eng"));

        var result = await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.False(result);
        Assert.Equal("existing", File.ReadAllText(existingPath));
        Assert.Equal(0, _ffmpegRuns);
    }

    [Fact]
    public async Task ExtractSubtitles_WithSameLanguageTwice_WritesOneFile()
    {
        SetStreams(Stream(2, "subrip", language: "eng"), Stream(3, "subrip", language: "eng"));

        await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.Equal(["Movie (2020).en.srt"], GetSubtitleFiles());
    }

    [Fact]
    public async Task ExtractSubtitles_WithUndefinedTag_UsesTitle()
    {
        SetStreams(Stream(2, "subrip", language: "und", title: "Français"));

        await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.Equal(["Movie (2020).fr.srt"], GetSubtitleFiles());
    }

    [Fact]
    public async Task ExtractSubtitles_WithoutTagOrTitle_SkipsStream()
    {
        SetStreams(Stream(2, "subrip"));

        var result = await _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None);

        Assert.False(result);
        Assert.Empty(GetSubtitleFiles());
        Assert.Equal(0, _ffmpegRuns);
    }

    [Fact]
    public async Task ExtractSubtitles_WithoutVideoFile_DoesNotRunFfprobe()
    {
        var result = await _service.ExtractSubtitles(_mediaDirectory, "Other Movie (2021)", CancellationToken.None);

        Assert.False(result);
        _processRunner.Verify(
            runner => runner.Run(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void GetMediaFileState_WithoutVideoFile_ReturnsMissing()
    {
        Assert.Equal("missing", _service.GetMediaFileState(_mediaDirectory, "Other Movie (2021)"));
    }

    [Fact]
    public void GetMediaFileState_WhenVideoFileChanges_ReturnsNewState()
    {
        var before = _service.GetMediaFileState(_mediaDirectory, FileName);
        File.WriteAllText(Path.Combine(_mediaDirectory, $"{FileName}.mkv"), "replaced");

        Assert.NotEqual(before, _service.GetMediaFileState(_mediaDirectory, FileName));
    }

    [Fact]
    public async Task ExtractSubtitles_WhenFfmpegFails_Throws()
    {
        SetStreams(Stream(2, "subrip", language: "eng"));
        _processRunner
            .Setup(runner => runner.Run(FfmpegPath, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, "", "Invalid data found"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExtractSubtitles(_mediaDirectory, FileName, CancellationToken.None));
    }

    private static object Stream(
        int index,
        string codec,
        string? language = null,
        string? title = null,
        bool forced = false,
        bool hearingImpaired = false)
    {
        var tags = new Dictionary<string, string>();
        if (language != null)
        {
            tags["language"] = language;
        }

        if (title != null)
        {
            tags["title"] = title;
        }

        return new
        {
            index,
            codec_name = codec,
            disposition = new { forced = forced ? 1 : 0, hearing_impaired = hearingImpaired ? 1 : 0 },
            tags
        };
    }

    private void SetStreams(params object[] streams)
    {
        _probeOutput = JsonSerializer.Serialize(new { streams });
    }

    private List<string> GetSubtitleFiles()
    {
        return Directory.GetFiles(_mediaDirectory)
            .Select(file => Path.GetFileName(file)!)
            .Where(fileName => fileName != $"{FileName}.mkv")
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
