using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using HomeSensors.Service.Emailing;
using VoidCore.Model.Time;

namespace HomeSensors.Service.Workers;

/// <summary>
/// This worker checks for limit outliers and sends an email.
/// </summary>
public class CheckTemperatureLimitsWorker : BackgroundService
{
    private readonly ILogger<CheckTemperatureLimitsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly TimeSpan _betweenTicks = TimeSpan.FromMinutes(20);
    private readonly TimeSpan _betweenAlerts = TimeSpan.FromHours(2);
    private const string ColdStatus = "cold";
    private const string HotStatus = "hot";
    private const string MinStatus = "minimum";
    private const string MaxStatus = "maximum";

    public CheckTemperatureLimitsWorker(ILogger<CheckTemperatureLimitsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService, EmailNotificationService emailNotificationService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _emailNotificationService = emailNotificationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        // Store previously sent alerts. We will only remind about them every few hours.
        var latchedAlerts = new List<Alert>();

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation($"{nameof(CheckTemperatureLimitsWorker)} job is starting.");
                var now = _dateTimeService.MomentWithOffset;

                using var scope = _scopeFactory.CreateScope();
                var locationRepository = scope.ServiceProvider.GetRequiredService<TemperatureLocationRepository>();
                var temperatureRepository = scope.ServiceProvider.GetRequiredService<TemperatureReadingRepository>();

                var sinceLastTick = now.Subtract(_betweenTicks);

                var failedResults = (await locationRepository.CheckLimits(sinceLastTick))
                    .Where(x => x.IsFailed)
                    .ToArray();

                await CleanClearedAlerts(temperatureRepository, latchedAlerts, failedResults, stoppingToken);
                CleanExpiredAlerts(latchedAlerts, now);
                await ProcessAlerts(latchedAlerts, now, failedResults, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(CheckTemperatureLimitsWorker));
            }
            finally
            {
                _logger.LogInformation($"{nameof(CheckTemperatureLimitsWorker)} job is finished.");
            }
        }
    }

    private async Task ProcessAlerts(List<Alert> latchedAlerts, DateTimeOffset now, CheckLimitResult[] failedResults, CancellationToken stoppingToken)
    {
        foreach (var failedResult in failedResults)
        {
            if (failedResult.MinReading is not null && !AlertExists(latchedAlerts, failedResult, ColdStatus))
            {
                await NotifyAlert(failedResult, MinStatus, ColdStatus, failedResult.MinReading, failedResult.Location.MinTemperatureLimitCelsius, stoppingToken);
                AddAlert(latchedAlerts, failedResult, ColdStatus, now);
            }

            if (failedResult.MaxReading is not null && !AlertExists(latchedAlerts, failedResult, HotStatus))
            {
                await NotifyAlert(failedResult, MaxStatus, HotStatus, failedResult.MaxReading, failedResult.Location.MaxTemperatureLimitCelsius, stoppingToken);
                AddAlert(latchedAlerts, failedResult, HotStatus, now);
            }
        }
    }

    private static bool AlertExists(List<Alert> latchedAlerts, CheckLimitResult failedResult, string status)
    {
        return latchedAlerts.Any(x => x.Result.Location.Id == failedResult.Location.Id && x.Status == status);
    }

    private void AddAlert(List<Alert> latchedAlerts, CheckLimitResult failedResult, string status, DateTimeOffset now)
    {
        latchedAlerts.Add(new Alert(failedResult, status, now.Add(_betweenAlerts)));
    }

    private async Task CleanClearedAlerts(TemperatureReadingRepository temperatureRepository, List<Alert> latchedAlerts, CheckLimitResult[] failedResults, CancellationToken stoppingToken)
    {
        var clearedAlerts = latchedAlerts
            .Where(alert => !failedResults.Any(x => AlertIsForThisResult(alert, x)))
            .ToArray();

        foreach (var alert in clearedAlerts)
        {
            var location = alert.Result.Location;

            var maybeReading = await temperatureRepository
                .GetCurrent(location.Id);

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

    private static void CleanExpiredAlerts(List<Alert> latchedAlerts, DateTimeOffset now)
    {
        var expiredAlerts = latchedAlerts
            .Where(x => x.Expiry <= now)
            .ToArray();

        foreach (var alert in expiredAlerts)
        {
            latchedAlerts.Remove(alert);
        }
    }

    private static bool AlertIsForThisResult(Alert alert, CheckLimitResult result)
    {
        return alert.Result.Location.Id == result.Location.Id &&
        (
            (alert.Status == ColdStatus && result.MinReading is not null) ||
            (alert.Status == HotStatus && result.MaxReading is not null)
        );
    }

    private Task NotifyAlert(CheckLimitResult failedResult, string minOrMax, string hotOrCold, Reading reading, double? limit, CancellationToken stoppingToken)
    {
        var locationName = failedResult.Location.Name;
        var time = reading.Time;
        var readingTempString = GetDualTempString(reading.TemperatureCelsius);
        var limitTempString = GetDualTempString(limit);

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

    private Task NotifyClear(Reading reading, Alert alert, double? limit, string minOrMax, CancellationToken stoppingToken)
    {
        var location = alert.Result.Location;

        var time = reading.Time;
        var readingTempString = GetDualTempString(reading.TemperatureCelsius);
        var limitTempString = GetDualTempString(limit);

        _logger.LogInformation("Temperature within limit: {LocationName} is no longer {HotOrCold}. Currently {Reading} at {Time}. Limit of {Limit}.", location.Name, alert.Status, readingTempString, time, limitTempString);

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject($"{location.Name} is no longer {alert.Status} ({readingTempString})");

            e.AddLine($"{location.Name} no longer exceeds the {minOrMax} temperature limit of {limitTempString}.");
            e.AddLine();
            e.AddLine($"Temperature: {readingTempString}");
            e.AddLine($"Time: {time}");
        }, stoppingToken);
    }

    private static string FormatTemp(double tempCelsius, bool useFahrenheit = false)
    {
        var decimals = useFahrenheit ? 0 : 1;
        var convertedTemp = useFahrenheit ? (tempCelsius * 1.8) + 32 : tempCelsius;
        var num = Math.Round(convertedTemp, decimals, MidpointRounding.AwayFromZero);

        var unit = useFahrenheit ? "°F" : "°C";

        return $"{num}{unit}";
    }

    private static string GetDualTempString(double? tempCelsius)
    {
        if (!tempCelsius.HasValue)
        {
            return "null";
        }

        return $"{FormatTemp(tempCelsius.Value, true)} / {FormatTemp(tempCelsius.Value)}";
    }

    private sealed record Alert(CheckLimitResult Result, string Status, DateTimeOffset Expiry);
}
