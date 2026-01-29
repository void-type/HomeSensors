using HomeSensors.Model.Notifications;

namespace HomeSensors.Web.Services.PushTemperatureCurrentReadings;

/// <summary>
/// This worker broadcasts current readings to all of the TemperatureHub SignalR clients.
/// </summary>
public class PushTemperatureCurrentReadingsWorker : BackgroundService
{
    private readonly TimeSpan _betweenTicks;
    private readonly ILogger<PushTemperatureCurrentReadingsWorker> _logger;
    private readonly ITemperatureHubNotifier _hubNotifier;

    public PushTemperatureCurrentReadingsWorker(ILogger<PushTemperatureCurrentReadingsWorker> logger,
        ITemperatureHubNotifier hubNotifier,
        PushTemperatureCurrentReadingsSettings workerSettings)
    {
        _logger = logger;
        _hubNotifier = hubNotifier;
        _betweenTicks = TimeSpan.FromSeconds(workerSettings.BetweenTicksSeconds);

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} seconds.",
            nameof(PushTemperatureCurrentReadingsWorker),
            workerSettings.BetweenTicksSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _hubNotifier.NotifyCurrentReadingsChangedAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(PushTemperatureCurrentReadingsWorker));
            }
        }
    }
}
