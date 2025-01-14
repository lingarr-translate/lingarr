using Lingarr.Core.Enum;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Models;

public class RecurringJobStatus
{
    public string Id { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    
    public string JobMethod { get; set; } = string.Empty;
    
    public DateTime? NextExecution { get; set; }
    public string? LastJobId { get; set; }
    public string? LastJobState { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? TimeZoneId { get; set; }
    public string CurrentState { get; set; } = JobStatus.Planned.GetDisplayName();
    public bool IsCurrentlyRunning { get; set; }
    public string? CurrentJobId { get; set; }
}