using Hangfire.Server;
using Hangfire.States;

namespace Lingarr.Server.Filters;

public class JobContextFilter : IServerFilter, IElectStateFilter
{
    private static readonly AsyncLocal<string> JobTypeName = new();
    private static readonly AsyncLocal<string> JobId = new();

    public void OnPerforming(PerformingContext context)
    {
        JobTypeName.Value = context.BackgroundJob.Job.Type.Name;
        JobId.Value = context.BackgroundJob.Id;
    }

    public void OnPerformed(PerformedContext context)
    {
        JobTypeName.Value = context.BackgroundJob.Job.Type.Name;
        JobId.Value = context.BackgroundJob.Id;
    }

    public void OnStateElection(ElectStateContext context)
    {
        JobTypeName.Value = context.BackgroundJob.Job.Type.Name;
        JobId.Value = context.BackgroundJob.Id;
    }
    
    public static string GetCurrentJobTypeName()
    {
        return JobTypeName.Value ?? string.Empty;
    }

    public static string GetCurrentJobId()
    {
        return JobId.Value ?? string.Empty;
    }
}