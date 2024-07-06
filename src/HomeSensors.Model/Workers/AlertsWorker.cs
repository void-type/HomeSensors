using HomeSensors.Model.Alerts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Workers;

/// <summary>
/// This worker checks for limit outliers and sends an email.
/// </summary>
public class AlertsWorker : BackgroundService
{
    private readonly ILogger<AlertsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _betweenTicks;
    private readonly TimeSpan _betweenNotifications;

    public AlertsWorker(ILogger<AlertsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService,
        AlertsSettings workerSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);
        _betweenNotifications = TimeSpan.FromMinutes(workerSettings.BetweenNotificationsMinutes);

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(AlertsWorker),
            workerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Store previously sent alerts. We will only remind about them every few hours.
        var latchedLimitAlerts = new List<TemperatureLimitAlert>();
        var latchedDeviceAlerts = new List<DeviceAlert>();

        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(AlertsWorker));

                var now = _dateTimeService.MomentWithOffset;
                var lastTick = now.Subtract(_betweenTicks);

                using var scope = _scopeFactory.CreateScope();

                var limitsService = scope.ServiceProvider.GetRequiredService<TemperatureLimitAlertService>();
                var devicesService = scope.ServiceProvider.GetRequiredService<DeviceAlertService>();

                await limitsService.Process(latchedLimitAlerts, now, lastTick, _betweenNotifications, stoppingToken);
                await devicesService.Process(latchedDeviceAlerts, now, _betweenNotifications, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(AlertsWorker));
            }
            finally
            {
                _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(AlertsWorker), Stopwatch.GetElapsedTime(startTime));
            }
        }
    }
}
