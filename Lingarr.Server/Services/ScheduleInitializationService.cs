using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services;

public class ScheduleInitializationService : IHostedService
{
    private readonly IScheduleService _scheduleService;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ScheduleInitializationService> _logger;

    public ScheduleInitializationService(
        IScheduleService scheduleService,
        IHostApplicationLifetime appLifetime,
        ILogger<ScheduleInitializationService> logger)
    {
        _scheduleService = scheduleService;
        _appLifetime = appLifetime;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(OnApplicationStarted);
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /// <summary>
    /// This method is called when the application starts and performs the initialization of the <see cref="_scheduleService"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Logs any exception that occurs during the initialization of the <see cref="_scheduleService"/>.</exception>
    private async void OnApplicationStarted()
    {
        try
        {
            _logger.LogInformation("Initializing ScheduleService...");
            await _scheduleService.Initialize();
            _logger.LogInformation("ScheduleService initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing ScheduleService.");
        }
    }
}