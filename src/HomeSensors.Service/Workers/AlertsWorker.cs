using VoidCore.Model.Time;

namespace HomeSensors.Service.Workers;

/// <summary>
/// This worker checks for limit outliers and sends an email.
/// </summary>
public class AlertsWorker : BackgroundService
{
    private readonly ILogger<AlertsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _betweenTicks = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _betweenNotifications = TimeSpan.FromMinutes(4);

    public AlertsWorker(ILogger<AlertsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Store previously sent alerts. We will only remind about them every few hours.
        var latchedLimitAlerts = new List<TemperatureLimitAlert>();
        var latchedDeviceAlerts = new List<DeviceAlert>();

        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation($"{nameof(AlertsWorker)} job is starting.");

                var now = _dateTimeService.MomentWithOffset;
                var lastTick = now.Subtract(_betweenTicks);

                using var scope = _scopeFactory.CreateScope();

                var limitsService = scope.ServiceProvider.GetRequiredService<CheckTemperatureLimitsService>();
                var devicesService = scope.ServiceProvider.GetRequiredService<CheckDevicesService>();

                await limitsService.Process(latchedLimitAlerts, now, lastTick, _betweenNotifications, stoppingToken);
                await devicesService.Process(latchedDeviceAlerts, now, _betweenNotifications, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(AlertsWorker));
            }
            finally
            {
                _logger.LogInformation($"{nameof(AlertsWorker)} job is finished.");
            }
        }
    }
}
