using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class FfmpegInstallJob
{
    private readonly IFfmpegService _ffmpegService;
    private readonly LingarrDbContext _dbContext;

    public FfmpegInstallJob(IFfmpegService ffmpegService, LingarrDbContext dbContext)
    {
        _ffmpegService = ffmpegService;
        _dbContext = dbContext;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 3)]
    [Queue("system")]
    public async Task Execute(bool clearMediaHash, CancellationToken cancellationToken)
    {
        await _ffmpegService.Install(cancellationToken);

        // The hash is cleared only after the install, so an automation run during the
        // download cannot store a hash for media that was never extracted.
        if (clearMediaHash)
        {
            await _dbContext.Movies.ExecuteUpdateAsync(
                movie => movie.SetProperty(property => property.MediaHash, string.Empty), cancellationToken);
            await _dbContext.Episodes.ExecuteUpdateAsync(
                episode => episode.SetProperty(property => property.MediaHash, string.Empty), cancellationToken);
        }
    }
}
