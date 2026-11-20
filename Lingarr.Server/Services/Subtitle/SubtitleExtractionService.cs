using System.Text.Json;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.Extraction;

namespace Lingarr.Server.Services.Subtitle;

public class SubtitleExtractionService : ISubtitleExtractionService
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".m4v", ".mov", ".avi", ".webm"
    };

    private static readonly HashSet<string> TextCodecs = new(StringComparer.OrdinalIgnoreCase)
    {
        "subrip", "srt", "ass", "ssa", "mov_text", "tx3g", "webvtt", "text"
    };

    private static readonly HashSet<string> HearingImpairedTitleWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "sdh", "cc"
    };

    private readonly IFfmpegService _ffmpegService;
    private readonly IProcessRunner _processRunner;
    private readonly SubtitleStreamLanguageService _subtitleStreamLanguageService;
    private readonly ILogger<SubtitleExtractionService> _logger;

    public SubtitleExtractionService(
        IFfmpegService ffmpegService,
        IProcessRunner processRunner,
        SubtitleStreamLanguageService subtitleStreamLanguageService,
        ILogger<SubtitleExtractionService> logger)
    {
        _ffmpegService = ffmpegService;
        _processRunner = processRunner;
        _subtitleStreamLanguageService = subtitleStreamLanguageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsAvailable()
    {
        return _ffmpegService.IsInstalled();
    }

    /// <inheritdoc />
    public string GetMediaFileState(string directory, string fileName)
    {
        var mediaPath = FindMediaFile(directory, fileName);
        if (mediaPath == null)
        {
            return "missing";
        }

        var fileInfo = new FileInfo(mediaPath);
        return $"{fileInfo.Name}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
    }

    /// <inheritdoc />
    public async Task<bool> ExtractSubtitles(string directory, string fileName, CancellationToken cancellationToken)
    {
        var mediaPath = FindMediaFile(directory, fileName);
        if (mediaPath == null)
        {
            _logger.LogWarning("No video file found for |Green|{FileName}|/Green| in {Directory}", fileName, directory);
            return false;
        }

        var claimedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingStreams = new List<PendingStream>();

        var streams = await GetSubtitleStreams(mediaPath, cancellationToken);
        foreach (var stream in streams.Where(stream => TextCodecs.Contains(stream.Codec)))
        {
            var language = _subtitleStreamLanguageService.ResolveLanguage(stream.Language, stream.Title);
            if (language == null)
            {
                _logger.LogInformation(
                    "Skipped embedded subtitle stream {StreamIndex} of {MediaPath}, the language is unknown",
                    stream.Index, mediaPath);
                continue;
            }

            var subtitlePath = CreateSubtitlePath(mediaPath, stream, language);
            if (!claimedPaths.Add(subtitlePath) || File.Exists(subtitlePath))
            {
                continue;
            }

            pendingStreams.Add(new PendingStream(stream, subtitlePath));
        }

        if (pendingStreams.Count == 0)
        {
            return false;
        }

        var workDirectory = Path.Combine(Path.GetTempPath(), "lingarr-extraction", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDirectory);
        try
        {
            var temporaryPaths = pendingStreams.ToDictionary(
                pending => pending.Stream.Index,
                pending => Path.Combine(workDirectory, $"{pending.Stream.Index}{GetSubtitleExtension(pending.Stream.Codec)}"));

            await ExtractStreams(mediaPath, temporaryPaths, pendingStreams, cancellationToken);

            var writtenCount = 0;
            foreach (var pending in pendingStreams)
            {
                if (MoveSubtitle(mediaPath, pending, temporaryPaths[pending.Stream.Index]))
                {
                    writtenCount++;
                }
            }

            return writtenCount > 0;
        }
        finally
        {
            Directory.Delete(workDirectory, recursive: true);
        }
    }

    private static string? FindMediaFile(string directory, string fileName)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        return Directory.EnumerateFiles(directory)
            .Where(file => VideoExtensions.Contains(Path.GetExtension(file)))
            .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == fileName);
    }

    private async Task<List<SubtitleStream>> GetSubtitleStreams(string mediaPath, CancellationToken cancellationToken)
    {
        string[] arguments =
        [
            "-v", "error",
            "-select_streams", "s",
            "-show_entries", "stream=index,codec_name:stream_tags=language,title:stream_disposition=forced,hearing_impaired",
            "-of", "json",
            mediaPath
        ];

        var result = await _processRunner.Run(_ffmpegService.FfprobePath, arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffprobe failed for '{mediaPath}': {result.Error.Trim()}");
        }

        var output = JsonSerializer.Deserialize<ProbeOutput>(result.Output) ?? new ProbeOutput();
        return output.Streams.Select(stream =>
        {
            var tags = new Dictionary<string, string>(stream.Tags ?? [], StringComparer.OrdinalIgnoreCase);
            return new SubtitleStream
            {
                Index = stream.Index,
                Codec = stream.CodecName ?? "unknown",
                Language = tags.GetValueOrDefault("language"),
                Title = tags.GetValueOrDefault("title"),
                Forced = stream.Disposition?.GetValueOrDefault("forced") == 1,
                HearingImpaired = stream.Disposition?.GetValueOrDefault("hearing_impaired") == 1
            };
        }).ToList();
    }

    private async Task ExtractStreams(
        string mediaPath,
        Dictionary<int, string> temporaryPaths,
        List<PendingStream> pendingStreams,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "-nostdin", "-hide_banner", "-loglevel", "error", "-y", "-i", mediaPath };
        foreach (var pending in pendingStreams)
        {
            arguments.AddRange(["-map", $"0:{pending.Stream.Index}", "-c:s", GetOutputCodec(pending.Stream.Codec)]);
            arguments.Add(temporaryPaths[pending.Stream.Index]);
        }

        _logger.LogInformation(
            "Extracting |Orange|{StreamCount}|/Orange| embedded subtitles from |Green|{MediaPath}|/Green|",
            pendingStreams.Count, mediaPath);
        var result = await _processRunner.Run(_ffmpegService.FfmpegPath, arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffmpeg failed for '{mediaPath}': {result.Error.Trim()}");
        }
    }

    private bool MoveSubtitle(string mediaPath, PendingStream pending, string temporaryPath)
    {
        if (!File.Exists(temporaryPath))
        {
            _logger.LogWarning("ffmpeg wrote no file for stream {StreamIndex} of {MediaPath}", pending.Stream.Index, mediaPath);
            return false;
        }

        // The copy goes to a hidden file in the target folder first, so a crash never leaves
        // a half written subtitle that the subtitle scan would pick up.
        var partialPath = Path.Combine(
            Path.GetDirectoryName(pending.SubtitlePath) ?? string.Empty,
            $".{Path.GetFileName(pending.SubtitlePath)}.tmp");
        File.Copy(temporaryPath, partialPath, overwrite: true);
        try
        {
            File.Move(partialPath, pending.SubtitlePath);
        }
        catch (IOException) when (File.Exists(pending.SubtitlePath))
        {
            File.Delete(partialPath);
            return false;
        }

        _logger.LogInformation("Extracted embedded subtitle to |Green|{SubtitlePath}|/Green|", pending.SubtitlePath);
        return true;
    }

    private static string CreateSubtitlePath(string mediaPath, SubtitleStream stream, string language)
    {
        var titleWords = stream.Title == null ? [] : SubtitleStreamLanguageService.SplitWords(stream.Title).ToList();
        var parts = new List<string> { Path.GetFileNameWithoutExtension(mediaPath), language };
        if (stream.Forced || titleWords.Contains("forced", StringComparer.OrdinalIgnoreCase))
        {
            parts.Add("forced");
        }
        else if (stream.HearingImpaired || titleWords.Any(HearingImpairedTitleWords.Contains))
        {
            parts.Add("sdh");
        }

        var directory = Path.GetDirectoryName(mediaPath) ?? string.Empty;
        return Path.Combine(directory, string.Join('.', parts) + GetSubtitleExtension(stream.Codec));
    }

    private static string GetSubtitleExtension(string codec)
    {
        return codec.ToLowerInvariant() is "ass" or "ssa" ? ".ass" : ".srt";
    }

    private static string GetOutputCodec(string codec)
    {
        return codec.ToLowerInvariant() switch
        {
            "ass" => "copy",
            "ssa" => "ass",
            _ => "srt"
        };
    }

    private record PendingStream(SubtitleStream Stream, string SubtitlePath);
}
