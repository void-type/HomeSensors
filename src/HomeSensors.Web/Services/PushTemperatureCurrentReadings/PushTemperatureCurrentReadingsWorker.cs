using HomeSensors.Model.Notifications;

namespace HomeSensors.Web.Services.PushTemperatureCurrentReadings;

/// <summary>
/// This worker broadcasts current readings to all of the TemperatureHub SignalR clients.
/// </summary>
public partial class PushTemperatureCurrentReadingsWorker : BackgroundService
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

        EnablingBackgroundJob(logger, nameof(PushTemperatureCurrentReadingsWorker), workerSettings.BetweenTicksSeconds);
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
                WorkerFailed(_logger, ex, nameof(PushTemperatureCurrentReadingsWorker));
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Enabling background job: {JobName} every {BetweenTicksMinutes} seconds.")]
    private static partial void EnablingBackgroundJob(ILogger logger, string jobName, int betweenTicksMinutes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown in {WorkerName}.")]
    private static partial void WorkerFailed(ILogger logger, Exception exception, string workerName);
}
