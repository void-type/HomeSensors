using HomeSensors.Model.Emailing;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.Extensions.Logging;

namespace HomeSensors.Model.Services.Temperature.Alert;

public class TemperatureLimitAlertService
{
    private const string ColdStatus = "cold";
    private const string HotStatus = "hot";
    private const string MinStatus = "minimum";
    private const string MaxStatus = "maximum";

    private readonly ILogger<TemperatureLimitAlertService> _logger;
    private readonly TemperatureLocationRepository _locationRepository;
    private readonly TemperatureReadingRepository _readingRepository;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly TemperatureAlertsSettings _alertSettings;

    public TemperatureLimitAlertService(ILogger<TemperatureLimitAlertService> logger,
        TemperatureLocationRepository locationRepository, TemperatureReadingRepository readingRepository,
        EmailNotificationService emailNotificationService, TemperatureAlertsSettings alertSettings)
    {
        _logger = logger;
        _locationRepository = locationRepository;
        _readingRepository = readingRepository;
        _emailNotificationService = emailNotificationService;
        _alertSettings = alertSettings;
    }

    public async Task ProcessAsync(List<TemperatureLimitAlert> latchedAlerts, DateTimeOffset now, DateTimeOffset since, bool isAveragingEnabled, CancellationToken stoppingToken)
    {
        var failedResults = (await _locationRepository.CheckLimitsAsync(since, isAveragingEnabled))
            .Where(x => x.IsFailed)
            .ToArray();

        await CleanClearedAlertsAsync(latchedAlerts, failedResults, isAveragingEnabled, stoppingToken);

        // Clean expired alerts
        latchedAlerts.RemoveAll(x => x.Expiry <= now);

        await ProcessAlertsAsync(latchedAlerts, now, failedResults, stoppingToken);
    }

    private async Task ProcessAlertsAsync(List<TemperatureLimitAlert> latchedAlerts, DateTimeOffset now, TemperatureCheckLimitResponse[] failedResults, CancellationToken stoppingToken)
    {
        var betweenNotifications = TimeSpan.FromMinutes(_alertSettings.BetweenNotificationsMinutes);

        foreach (var failedResult in failedResults)
        {
            if (failedResult.MinReading is not null && !AlertExists(latchedAlerts, failedResult, ColdStatus))
            {
                await NotifyAlertAsync(failedResult, MinStatus, ColdStatus, failedResult.MinReading, failedResult.Location.MinTemperatureLimitCelsius, stoppingToken);
                AddAlert(latchedAlerts, failedResult, ColdStatus, now, betweenNotifications);
            }

            if (failedResult.MaxReading is not null && !AlertExists(latchedAlerts, failedResult, HotStatus))
            {
                await NotifyAlertAsync(failedResult, MaxStatus, HotStatus, failedResult.MaxReading, failedResult.Location.MaxTemperatureLimitCelsius, stoppingToken);
                AddAlert(latchedAlerts, failedResult, HotStatus, now, betweenNotifications);
            }
        }
    }

    private static bool AlertExists(List<TemperatureLimitAlert> latchedAlerts, TemperatureCheckLimitResponse failedResult, string status)
    {
        return latchedAlerts.Exists(x => x.Result.Location.Id == failedResult.Location.Id && x.Status == status);
    }

    private static void AddAlert(List<TemperatureLimitAlert> latchedAlerts, TemperatureCheckLimitResponse failedResult, string status, DateTimeOffset now, TimeSpan betweenAlerts)
    {
        latchedAlerts.Add(new TemperatureLimitAlert(failedResult, status, now.Add(betweenAlerts)));
    }

    private async Task CleanClearedAlertsAsync(List<TemperatureLimitAlert> latchedAlerts, TemperatureCheckLimitResponse[] failedResults, bool isAveragingEnabled, CancellationToken stoppingToken)
    {
        var clearedAlerts = latchedAlerts
            .Where(alert => !Array.Exists(failedResults, x => AlertIsForThisResult(alert, x)))
            .ToArray();

        foreach (var alert in clearedAlerts)
        {
            var location = alert.Result.Location;

            var maybeReading = await _readingRepository
                .GetCurrentForLocationAsync(location.Id, stoppingToken);

            if (maybeReading.HasNoValue)
            {
                continue;
            }

            var reading = maybeReading.Value;

            var alertIsCold = alert.Status == ColdStatus;

            // If not using averages, only clear if the reading is within the limit.
            if (!isAveragingEnabled && ((alertIsCold && reading.IsCold) || (!alertIsCold && reading.IsHot)))
            {
                continue;
            }

            var limit = alertIsCold ? alert.Result.Location.MinTemperatureLimitCelsius : alert.Result.Location.MaxTemperatureLimitCelsius;
            var minOrMax = alertIsCold ? MinStatus : MaxStatus;

            await NotifyClearAsync(reading, alert, limit, minOrMax, stoppingToken);
            latchedAlerts.Remove(alert);
        }
    }

    private static bool AlertIsForThisResult(TemperatureLimitAlert alert, TemperatureCheckLimitResponse result)
    {
        return alert.Result.Location.Id == result.Location.Id &&
        (
            (alert.Status == ColdStatus && result.MinReading is not null) ||
            (alert.Status == HotStatus && result.MaxReading is not null)
        );
    }

    private async Task NotifyAlertAsync(TemperatureCheckLimitResponse failedResult, string minOrMax, string hotOrCold, TemperatureReadingResponse reading, double? limit, CancellationToken stoppingToken)
    {
        var locationName = failedResult.Location.Name;
        var time = reading.Time;
        var readingTempString = TemperatureHelpers.GetDualTempString(reading.TemperatureCelsius);
        var limitTempString = TemperatureHelpers.GetDualTempString(limit);

        var lookBackMinutes = _alertSettings.LookBackMinutes;

        _logger.LogWarning("Temperature limit exceeded: {LocationName} was as {HotOrCold} as {Reading} at {Time}. Limit of {Limit}. Look back period: {LookBackMinutes} minutes.",
            locationName,
            hotOrCold,
            readingTempString,
            time,
            limitTempString,
            lookBackMinutes);

        await _emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"{locationName} is {hotOrCold} ({readingTempString})");

            e.AddLine($"{locationName} exceeded the {minOrMax} temperature limit of {limitTempString}.");
            if (lookBackMinutes > 0)
            {
                e.AddLine($"This alert is based on an average temperature over the last {lookBackMinutes} minutes.");
            }
            e.AddLine();
            e.AddLine($"Temperature: {readingTempString}");
            e.AddLine($"Time: {time}");
        }, stoppingToken);
    }

    private async Task NotifyClearAsync(TemperatureReadingResponse reading, TemperatureLimitAlert alert, double? limit, string minOrMax, CancellationToken stoppingToken)
    {
        var location = alert.Result.Location;

        var time = reading.Time;
        var readingTempString = TemperatureHelpers.GetDualTempString(reading.TemperatureCelsius);
        var limitTempString = TemperatureHelpers.GetDualTempString(limit);

        var lookBackMinutes = _alertSettings.LookBackMinutes;

        _logger.LogWarning("Temperature within limit: {LocationName} is no longer {HotOrCold}. Currently {Reading} at {Time}. Limit of {Limit}. Look back period: {LookBackMinutes} minutes.",
            location.Name,
            alert.Status,
            readingTempString,
            time,
            limitTempString,
            lookBackMinutes);

        await _emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"{location.Name} is no longer {alert.Status} ({readingTempString})");

            e.AddLine($"{location.Name} no longer exceeds the {minOrMax} temperature limit of {limitTempString}.");
            if (lookBackMinutes > 0)
            {
                e.AddLine($"This alert is based on an average temperature over the last {lookBackMinutes} minutes.");
            }
            e.AddLine();
            e.AddLine($"Temperature: {readingTempString}");
            e.AddLine($"Time: {time}");
        }, stoppingToken);
    }
}
