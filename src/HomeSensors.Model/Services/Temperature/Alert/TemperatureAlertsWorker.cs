using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Services.Temperature.Alert;

/// <summary>
/// This worker checks for limit outliers and sends an email.
/// </summary>
public class TemperatureAlertsWorker : BackgroundService
{
    private readonly ILogger<TemperatureAlertsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _betweenTicks;
    private readonly TimeSpan _betweenNotifications;

    public TemperatureAlertsWorker(ILogger<TemperatureAlertsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService,
        TemperatureAlertsSettings workerSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);
        _betweenNotifications = TimeSpan.FromMinutes(workerSettings.BetweenNotificationsMinutes);

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(TemperatureAlertsWorker),
            workerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Store previously sent alerts. We will only remind about them every few hours.
        var latchedLimitAlerts = new List<TemperatureLimitAlert>();
        var latchedDeviceAlerts = new List<TemperatureDeviceAlert>();

        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(TemperatureAlertsWorker));

                var now = _dateTimeService.MomentWithOffset;
                var lastTick = now.Subtract(_betweenTicks);

                using var scope = _scopeFactory.CreateScope();

                var limitsService = scope.ServiceProvider.GetRequiredService<TemperatureLimitAlertService>();
                var devicesService = scope.ServiceProvider.GetRequiredService<TemperatureDeviceAlertService>();

                await limitsService.Process(latchedLimitAlerts, now, lastTick, _betweenNotifications, stoppingToken);
                await devicesService.Process(latchedDeviceAlerts, now, _betweenNotifications, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(TemperatureAlertsWorker));
            }
            finally
            {
                _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(TemperatureAlertsWorker), Stopwatch.GetElapsedTime(startTime));
            }
        }
    }
}
