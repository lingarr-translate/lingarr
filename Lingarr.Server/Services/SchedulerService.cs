using Quartz;
using Quartz.Impl.Matchers;
using Lingarr.Server.Jobs;

namespace Lingarr.Server.Services;

public class SchedulerService
{
    private IScheduler _scheduler;
    private ILogger<SchedulerService> _logger;

    public SchedulerService(IScheduler scheduler, ILogger<SchedulerService> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task StartAsync(int intervalInSeconds)
    {
        await _scheduler.Start();

        IJobDetail job = JobBuilder.Create<TranslationJob>().Build();
        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("TranslationJob")
            .WithSimpleSchedule(simpleScheduleBuilder =>
                simpleScheduleBuilder.WithIntervalInSeconds(intervalInSeconds).RepeatForever())
            .Build();

        _logger.LogInformation($"Started job: {job.Key}");
        await _scheduler.ScheduleJob(job, trigger);

    }

    public async Task StopAsync()
    {
        var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        foreach (var jobKey in jobKeys)
        {
            _logger.LogInformation($"Stopped job: {jobKey}");
            await _scheduler.DeleteJob(jobKey);

        }
    }

    public async Task UpdateInterval(int intervalInSeconds)
    {
        await StopAsync();
        await StartAsync(intervalInSeconds);
    }
}
