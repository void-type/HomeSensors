using HomeSensors.Model.Temperature.Models;
using HomeSensors.Model.Temperature.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HomeSensors.Model.Temperature.Workers;

/// <summary>
/// This worker checks for limit outliers and sends an email.
/// </summary>
public class TemperatureAlertsWorker : BackgroundService
{
    private readonly ILogger<TemperatureAlertsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly TemperatureAlertsSettings _alertSettings;

    // Store previously sent alerts. We will only remind about them every few hours.
    private readonly List<TemperatureLimitAlert> _latchedLimitAlerts = [];
    private readonly List<TemperatureDeviceAlert> _latchedDeviceAlerts = [];
    private DateTimeOffset _lastTick;

    public TemperatureAlertsWorker(ILogger<TemperatureAlertsWorker> logger, IServiceScopeFactory scopeFactory, TimeProvider timeProvider,
        TemperatureAlertsSettings alertSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _alertSettings = alertSettings;

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(TemperatureAlertsWorker),
            alertSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var betweenTicks = TimeSpan.FromMinutes(_alertSettings.BetweenTicksMinutes);
        var timer = new PeriodicTimer(betweenTicks);
        _lastTick = _timeProvider.GetUtcNow();

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(TemperatureAlertsWorker));

                await CheckAlertsAsync(stoppingToken);
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

    private async Task CheckAlertsAsync(CancellationToken stoppingToken)
    {
        var now = _timeProvider.GetUtcNow();

        using var scope = _scopeFactory.CreateScope();

        var limitsService = scope.ServiceProvider.GetRequiredService<TemperatureLimitAlertService>();
        var devicesService = scope.ServiceProvider.GetRequiredService<TemperatureDeviceAlertService>();

        var isAveragingEnabled = _alertSettings.LookBackMinutes > 0;

        var since = isAveragingEnabled
            ? now.Subtract(TimeSpan.FromMinutes(_alertSettings.LookBackMinutes))
            : _lastTick;

        await limitsService.ProcessAsync(_latchedLimitAlerts, now, since, isAveragingEnabled, stoppingToken);
        await devicesService.ProcessAsync(_latchedDeviceAlerts, now, stoppingToken);

        _lastTick = now;
    }
}
