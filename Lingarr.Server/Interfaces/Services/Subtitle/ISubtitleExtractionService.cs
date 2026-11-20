namespace Lingarr.Server.Interfaces.Services.Subtitle;

public interface ISubtitleExtractionService
{
    /// <summary>
    /// Checks that ffmpeg is installed, so embedded subtitles can be extracted.
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Gets the name, size and change time of the video file, or "missing" when it does not exist.
    /// </summary>
    string GetMediaFileState(string directory, string fileName);

    /// <summary>
    /// Extracts the embedded text subtitles of a media file and writes them next to it.
    /// Returns true when at least one subtitle file was written.
    /// </summary>
    Task<bool> ExtractSubtitles(string directory, string fileName, CancellationToken cancellationToken);
}
