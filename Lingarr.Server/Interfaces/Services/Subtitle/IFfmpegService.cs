namespace Lingarr.Server.Interfaces.Services.Subtitle;

public interface IFfmpegService
{
    string FfmpegPath { get; }
    string FfprobePath { get; }

    /// <summary>
    /// Checks that ffmpeg and ffprobe exist in the ffmpeg folder.
    /// </summary>
    bool IsInstalled();

    /// <summary>
    /// Downloads ffmpeg and ffprobe when they are missing or an older download.
    /// </summary>
    Task Install(CancellationToken cancellationToken);
}
