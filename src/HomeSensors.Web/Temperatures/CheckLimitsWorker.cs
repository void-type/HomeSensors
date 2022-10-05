using HomeSensors.Data;
using HomeSensors.Data.Repositories;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Temperatures;

/// <summary>
/// This worker broadcasts all of the TemperatureHub SignalR clients with current readings.
/// </summary>
public class CheckLimitsWorker : BackgroundService
{
    private readonly TimeSpan _betweenTicks = TimeSpan.FromMinutes(20);
    private readonly ILogger<CheckLimitsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;

    public CheckLimitsWorker(ILogger<CheckLimitsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
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
                    _logger.LogWarning("Temperature limit check alert: {Location} was as low as {MinReading} at {Time}.", result.Location.Name, GetCelsiusString(reading), reading.Time);
                }

                if (result.MaxReading is not null)
                {
                    var reading = result.MaxReading;
                    _logger.LogWarning("Temperature limit check alert: {Location} was as high as {MaxReading} at {Time}.", result.Location.Name, GetCelsiusString(reading), reading.Time);
                }
            }
        }
    }

    private static string GetCelsiusString(TemperatureReading reading)
    {
        return reading.TemperatureCelsius?.ToString("0.#°C") ?? "null";
    }
}
