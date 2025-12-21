using Hangfire;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class TelemetryJob
{
    private readonly ITelemetryService _telemetryService;
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<TelemetryJob> _logger;

    public TelemetryJob(
        ITelemetryService telemetryService,
        IScheduleService scheduleService,
        ILogger<TelemetryJob> logger)
    {
        _telemetryService = telemetryService;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    [Queue("system")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

        try
        {
            if (!await _telemetryService.CanSubmitTelemetry())
            {
                await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
                return;
            }

            var payload = await _telemetryService.GenerateTelemetryPayload();
            var success = await _telemetryService.SubmitTelemetry(payload);
            if (success)
            {
                await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
            }
            else
            {
                await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
            }
        }
        catch (Exception ex)
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
            throw;
        }
    }
}
