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

    public async Task Process(List<TemperatureLimitAlert> latchedAlerts, DateTimeOffset now, DateTimeOffset lastTick, CancellationToken stoppingToken)
    {
        var failedResults = (await _locationRepository.CheckLimits(lastTick, _alertSettings.AverageIntervalMinutes))
            .Where(x => x.IsFailed)
            .ToArray();

        await CleanClearedAlerts(latchedAlerts, failedResults, stoppingToken);

        // Clean expired alerts
        latchedAlerts.RemoveAll(x => x.Expiry <= now);

        await ProcessAlerts(latchedAlerts, now, failedResults, stoppingToken);
    }

    private async Task ProcessAlerts(List<TemperatureLimitAlert> latchedAlerts, DateTimeOffset now, TemperatureCheckLimitResponse[] failedResults, CancellationToken stoppingToken)
    {
        var betweenNotifications = TimeSpan.FromMinutes(_alertSettings.BetweenNotificationsMinutes);

        foreach (var failedResult in failedResults)
        {
            if (failedResult.MinReading is not null && !AlertExists(latchedAlerts, failedResult, ColdStatus))
            {
                await NotifyAlert(failedResult, MinStatus, ColdStatus, failedResult.MinReading, failedResult.Location.MinTemperatureLimitCelsius, stoppingToken);
                AddAlert(latchedAlerts, failedResult, ColdStatus, now, betweenNotifications);
            }

            if (failedResult.MaxReading is not null && !AlertExists(latchedAlerts, failedResult, HotStatus))
            {
                await NotifyAlert(failedResult, MaxStatus, HotStatus, failedResult.MaxReading, failedResult.Location.MaxTemperatureLimitCelsius, stoppingToken);
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

    private async Task CleanClearedAlerts(List<TemperatureLimitAlert> latchedAlerts, TemperatureCheckLimitResponse[] failedResults, CancellationToken stoppingToken)
    {
        var clearedAlerts = latchedAlerts
            .Where(alert => !Array.Exists(failedResults, x => AlertIsForThisResult(alert, x)))
            .ToArray();

        foreach (var alert in clearedAlerts)
        {
            var location = alert.Result.Location;

            var maybeReading = await _readingRepository
                .GetCurrentForLocation(location.Id, stoppingToken);

            if (maybeReading.HasNoValue)
            {
                continue;
            }

            var reading = maybeReading.Value;

            var alertIsCold = alert.Status == ColdStatus;

            if ((alertIsCold && reading.IsCold) || (!alertIsCold && reading.IsHot))
            {
                continue;
            }

            var limit = alertIsCold ? alert.Result.Location.MinTemperatureLimitCelsius : alert.Result.Location.MaxTemperatureLimitCelsius;
            var minOrMax = alertIsCold ? MinStatus : MaxStatus;

            await NotifyClear(reading, alert, limit, minOrMax, stoppingToken);
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

    private Task NotifyAlert(TemperatureCheckLimitResponse failedResult, string minOrMax, string hotOrCold, TemperatureReadingResponse reading, double? limit, CancellationToken stoppingToken)
    {
        var locationName = failedResult.Location.Name;
        var time = reading.Time;
        var readingTempString = TemperatureHelpers.GetDualTempString(reading.TemperatureCelsius);
        var limitTempString = TemperatureHelpers.GetDualTempString(limit);

        _logger.LogWarning("Temperature limit exceeded: {LocationName} was as {HotOrCold} as {Reading} at {Time}. Limit of {Limit}.", locationName, hotOrCold, readingTempString, time, limitTempString);

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject($"{locationName} is {hotOrCold} ({readingTempString})");

            e.AddLine($"{locationName} exceeded the {minOrMax} temperature limit of {limitTempString}.");
            e.AddLine();
            e.AddLine($"Temperature: {readingTempString}");
            e.AddLine($"Time: {time}");
        }, stoppingToken);
    }

    private Task NotifyClear(TemperatureReadingResponse reading, TemperatureLimitAlert alert, double? limit, string minOrMax, CancellationToken stoppingToken)
    {
        var location = alert.Result.Location;

        var time = reading.Time;
        var readingTempString = TemperatureHelpers.GetDualTempString(reading.TemperatureCelsius);
        var limitTempString = TemperatureHelpers.GetDualTempString(limit);

        _logger.LogWarning("Temperature within limit: {LocationName} is no longer {HotOrCold}. Currently {Reading} at {Time}. Limit of {Limit}.", location.Name, alert.Status, readingTempString, time, limitTempString);

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject($"{location.Name} is no longer {alert.Status} ({readingTempString})");

            e.AddLine($"{location.Name} no longer exceeds the {minOrMax} temperature limit of {limitTempString}.");
            e.AddLine();
            e.AddLine($"Temperature: {readingTempString}");
            e.AddLine($"Time: {time}");
        }, stoppingToken);
    }
}
