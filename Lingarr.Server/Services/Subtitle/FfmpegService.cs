using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using SharpCompress.Readers;

namespace Lingarr.Server.Services.Subtitle;

public class FfmpegService : IFfmpegService
{
    private const string Version = "8.1.3-2";
    private const string VersionFileName = "version";
    private static readonly string[] ProgramNames = ["ffmpeg", "ffprobe"];
    private static readonly SemaphoreSlim InstallLock = new(1, 1);

    private static readonly Dictionary<Architecture, (string Url, string Sha256)> Downloads = new()
    {
        {
            Architecture.X64,
            ($"https://github.com/jellyfin/jellyfin-ffmpeg/releases/download/v{Version}/jellyfin-ffmpeg_{Version}_portable_linux64-gpl.tar.xz",
                "1df84d04c4322902dba81c290508cdf3d89524d210b9fe03154bea6c4e3bfbb6")
        },
        {
            Architecture.Arm64,
            ($"https://github.com/jellyfin/jellyfin-ffmpeg/releases/download/v{Version}/jellyfin-ffmpeg_{Version}_portable_linuxarm64-gpl.tar.xz",
                "6522585818059cc886363fb1dee86ac9b8579fec3b914337fb92974ef9eb9884")
        }
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FfmpegService> _logger;
    private readonly string _directory;

    public FfmpegService(
        IHttpClientFactory httpClientFactory,
        IProcessRunner processRunner,
        ILogger<FfmpegService> logger,
        string directory)
    {
        _httpClientFactory = httpClientFactory;
        _processRunner = processRunner;
        _logger = logger;
        _directory = directory;
    }

    public string FfmpegPath => Path.Combine(_directory, "ffmpeg");
    public string FfprobePath => Path.Combine(_directory, "ffprobe");
    private string VersionFilePath => Path.Combine(_directory, VersionFileName);

    /// <inheritdoc />
    public bool IsInstalled()
    {
        return File.Exists(FfmpegPath) && File.Exists(FfprobePath);
    }

    /// <inheritdoc />
    public async Task Install(CancellationToken cancellationToken)
    {
        await InstallLock.WaitAsync(cancellationToken);
        try
        {
            if (IsInstalled() && !IsOlderDownload())
            {
                return;
            }

            if (!OperatingSystem.IsLinux() || !Downloads.TryGetValue(RuntimeInformation.OSArchitecture, out var download))
            {
                throw new PlatformNotSupportedException(
                    $"No ffmpeg download exists for {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}. " +
                    $"Copy ffmpeg and ffprobe into {_directory} to extract subtitles.");
            }

            Directory.CreateDirectory(_directory);
            var archivePath = Path.Combine(_directory, "ffmpeg.tar.xz.tmp");
            try
            {
                _logger.LogInformation("Downloading ffmpeg {Version} to {Directory}", Version, _directory);
                await Download(download.Url, archivePath, cancellationToken);
                await VerifyChecksum(archivePath, download.Sha256, cancellationToken);
                ExtractPrograms(archivePath);
                await File.WriteAllTextAsync(VersionFilePath, Version, cancellationToken);
            }
            finally
            {
                File.Delete(archivePath);
            }

            await VerifyStart(cancellationToken);
            _logger.LogInformation("Installed ffmpeg {Version} in {Directory}", Version, _directory);
        }
        finally
        {
            InstallLock.Release();
        }
    }

    private bool IsOlderDownload()
    {
        // Without a version file the programs were copied in by the user, those are never replaced.
        return File.Exists(VersionFilePath) && File.ReadAllText(VersionFilePath).Trim() != Version;
    }

    private async Task Download(string url, string archivePath, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(archivePath);
        await source.CopyToAsync(target, cancellationToken);
    }

    private static async Task VerifyChecksum(string archivePath, string expectedSha256, CancellationToken cancellationToken)
    {
        await using var archive = File.OpenRead(archivePath);
        var sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(archive, cancellationToken));
        if (sha256 != expectedSha256)
        {
            throw new InvalidDataException($"The ffmpeg download has checksum {sha256}, expected {expectedSha256}.");
        }
    }

    private void ExtractPrograms(string archivePath)
    {
        using var archive = File.OpenRead(archivePath);
        using var reader = ReaderFactory.OpenReader(archive, new ReaderOptions());
        while (reader.MoveToNextEntry())
        {
            var programName = Path.GetFileName(reader.Entry.Key);
            if (reader.Entry.IsDirectory || !ProgramNames.Contains(programName))
            {
                continue;
            }

            var temporaryPath = Path.Combine(_directory, $"{programName}.tmp");
            using (var target = File.Create(temporaryPath))
            {
                reader.WriteEntryTo(target);
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temporaryPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            File.Move(temporaryPath, Path.Combine(_directory, programName), overwrite: true);
        }

        if (!IsInstalled())
        {
            throw new InvalidDataException("The ffmpeg download does not contain ffmpeg and ffprobe.");
        }
    }

    private async Task VerifyStart(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.Run(FfmpegPath, ["-version"], cancellationToken);
            if (result.ExitCode == 0)
            {
                return;
            }
        }
        catch (Win32Exception exception)
        {
            _logger.LogWarning(exception, "ffmpeg in {Directory} cannot start", _directory);
        }

        // Some NAS systems mount volumes with noexec. Removing the programs keeps extraction off
        // instead of failing on every media file.
        foreach (var programName in ProgramNames)
        {
            File.Delete(Path.Combine(_directory, programName));
        }

        throw new InvalidOperationException(
            $"ffmpeg was downloaded to {_directory} but cannot start. If this folder is mounted with noexec, " +
            "set FFMPEG_PATH to a folder that allows programs to run.");
    }
}
