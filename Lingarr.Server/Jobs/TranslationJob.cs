using Quartz;

namespace Lingarr.Server.Jobs;

public class TranslationJob: IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // This is where your job logic would go.
        Console.WriteLine($"Scheduled job run: {context.JobDetail.Key} interval: {context.ScheduledFireTimeUtc} - {context.NextFireTimeUtc}");
        return Task.CompletedTask;
    }

}
