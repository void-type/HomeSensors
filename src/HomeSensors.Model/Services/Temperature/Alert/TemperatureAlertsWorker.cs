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
    private readonly TemperatureAlertsSettings _alertSettings;

    public TemperatureAlertsWorker(ILogger<TemperatureAlertsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService,
        TemperatureAlertsSettings alertSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _alertSettings = alertSettings;

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(TemperatureAlertsWorker),
            alertSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Store previously sent alerts. We will only remind about them every few hours.
        var latchedLimitAlerts = new List<TemperatureLimitAlert>();
        var latchedDeviceAlerts = new List<TemperatureDeviceAlert>();

        var betweenTicks = TimeSpan.FromMinutes(_alertSettings.BetweenTicksMinutes);
        var timer = new PeriodicTimer(betweenTicks);
        var lastTick = _dateTimeService.MomentWithOffset;

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(TemperatureAlertsWorker));

                var now = _dateTimeService.MomentWithOffset;

                using var scope = _scopeFactory.CreateScope();

                var limitsService = scope.ServiceProvider.GetRequiredService<TemperatureLimitAlertService>();
                var devicesService = scope.ServiceProvider.GetRequiredService<TemperatureDeviceAlertService>();

                var isAveragingEnabled = _alertSettings.LookBackMinutes > 0;

                var since = isAveragingEnabled
                    ? now.Subtract(TimeSpan.FromMinutes(_alertSettings.LookBackMinutes))
                    : lastTick;

                await limitsService.ProcessAsync(latchedLimitAlerts, now, since, isAveragingEnabled, stoppingToken);
                await devicesService.ProcessAsync(latchedDeviceAlerts, now, stoppingToken);

                lastTick = now;
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
