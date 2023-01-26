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
    private readonly TimeSpan _betweenTicks = TimeSpan.FromMinutes(20);
    private readonly ILogger<CheckTemperatureLimitsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailNotificationService _emailNotificationService;

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

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var locationRepository = scope.ServiceProvider.GetRequiredService<TemperatureLocationRepository>();

                var sinceLastTick = _dateTimeService.MomentWithOffset.Subtract(_betweenTicks);

                var failedResults = (await locationRepository.CheckLimits(sinceLastTick))
                    .Where(x => x.IsFailed);

                foreach (var failedResult in failedResults)
                {
                    if (failedResult.MinReading is not null)
                    {
                        await NotifyLimitExceeded(failedResult, "minimum", "cold", failedResult.MinReading, failedResult.Location.MinTemperatureLimitCelsius, stoppingToken);
                    }

                    if (failedResult.MaxReading is not null)
                    {
                        await NotifyLimitExceeded(failedResult, "maximum", "hot", failedResult.MaxReading, failedResult.Location.MaxTemperatureLimitCelsius, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(CheckTemperatureLimitsWorker));
            }
        }
    }

    private Task NotifyLimitExceeded(CheckLimitResult failedResult, string minOrMax, string hotOrCold, Reading reading, double? limit, CancellationToken stoppingToken)
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
}
