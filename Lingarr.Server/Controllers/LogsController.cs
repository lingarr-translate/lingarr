using System.Text.Json;
using Lingarr.Server.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    [HttpGet("stream")]
    public async Task GetLogStreamAsync(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        foreach (var log in InMemoryLogSink.GetRecentLogs(100))
        {
            string json = JsonSerializer.Serialize(log);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        }
        await Response.Body.FlushAsync(cancellationToken);
        
        var logQueue = InMemoryLogSink.LogQueue;
        var lastProcessedCount = logQueue.Count;
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        
        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            var currentCount = logQueue.Count;
            if (currentCount > lastProcessedCount)
            {
                var newLogs = logQueue.TakeLast(currentCount - lastProcessedCount);
                foreach (var log in newLogs)
                {
                    string json = JsonSerializer.Serialize(log);
                    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                }
                await Response.Body.FlushAsync(cancellationToken);
            }
            lastProcessedCount = currentCount;
        }
    }
}
