using HomeSensors.Data;
using HomeSensors.Data.Repositories;
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
            using var scope = _scopeFactory.CreateScope();
            var locationRepository = scope.ServiceProvider.GetRequiredService<TemperatureLocationRepository>();

            var results = await locationRepository.CheckLimits(_dateTimeService.MomentWithOffset.Subtract(_betweenTicks));

            foreach (var result in results.Where(x => x.IsFailed))
            {
                if (result.MinReading is not null)
                {
                    var reading = result.MinReading;
                    var tempString = GetCelsiusString(reading);

                    _logger.LogWarning("Temperature limit check alert: {Location} was as cold as {MinReading} at {Time}.", result.Location.Name, tempString, reading.Time);

                    await _emailNotificationService.Send(e =>
                    {
                        e.SetSubject($"{result.Location.Name} is cold ({tempString})");

                        e.AddLine($"Temperature limit check alert: {result.Location.Name} was as cold as {tempString} at {reading.Time}.");
                    }, stoppingToken);
                }

                if (result.MaxReading is not null)
                {
                    var reading = result.MaxReading;
                    var tempString = GetCelsiusString(reading);

                    _logger.LogWarning("Temperature limit check alert: {Location} was as hot as {MaxReading} at {Time}.", result.Location.Name, tempString, reading.Time);

                    await _emailNotificationService.Send(e =>
                    {
                        e.SetSubject($"{result.Location.Name} is hot ({tempString})");

                        e.AddLine($"Temperature limit check alert: {result.Location.Name} was as hot as {tempString} at {reading.Time}.");
                    }, stoppingToken);
                }
            }
        }
    }

    private static string GetCelsiusString(TemperatureReading reading)
    {
        return reading.TemperatureCelsius?.ToString("0.#°C") ?? "null";
    }
}
