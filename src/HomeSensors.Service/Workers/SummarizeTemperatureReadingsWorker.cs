using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Helpers;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Service.Workers;

/// <summary>
/// This worker compresses older data by summarizing into regular average intervals.
/// </summary>
public class SummarizeTemperatureReadingsWorker : BackgroundService
{
    private readonly TimeSpan _betweenTicks = TimeSpan.FromMinutes(120);
    private readonly TimeSpan _summarizeCutoff = TimeSpan.FromDays(-30);
    private const int SummarizeIntervalMinutes = 5;
    private readonly ILogger<SummarizeTemperatureReadingsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;

    public SummarizeTemperatureReadingsWorker(ILogger<SummarizeTemperatureReadingsWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeService dateTimeService)
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
            var data = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

            var cutoffLimit = _dateTimeService.MomentWithOffset
                .Add(_summarizeCutoff)
                .RoundDownMinutes(SummarizeIntervalMinutes);

            var devices = await data.TemperatureDevices.ToListAsync(stoppingToken);

            var oldReadings = await data.TemperatureReadings
                .AsNoTracking()
                .WhereShouldBeSummarized(cutoffLimit)
                .ToListAsync(stoppingToken);

            var newReadings = oldReadings
                .GroupBy(x => (device: x.TemperatureDeviceId, location: x.TemperatureLocationId))
                .SelectMany(group => group
                    .GroupBy(x => x.Time.RoundDownMinutes(SummarizeIntervalMinutes))
                    .Select(x => new TemperatureReading()
                    {
                        Time = x.Key,
                        DeviceBatteryLevel = null,
                        DeviceStatus = null,
                        Humidity = x.Average(x => x.Humidity),
                        TemperatureCelsius = x.Average(x => x.TemperatureCelsius),
                        IsSummary = true,
                        TemperatureDeviceId = group.Key.device,
                        TemperatureDevice = devices.First(x => x.Id == group.Key.device),
                        TemperatureLocationId = group.Key.location
                    }))
                .ToList();

            _logger.LogInformation("Summarizing data older than {Cutoff}. Compressed {OldCount} rows to {NewCount} rows.", cutoffLimit.ToString("o"), oldReadings.Count, newReadings.Count);

            data.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            data.TemperatureReadings.AddRange(newReadings);
            await data.SaveChangesAsync(stoppingToken);

            await data.TemperatureReadings
                .WhereShouldBeSummarized(cutoffLimit)
                .ExecuteDeleteAsync(stoppingToken);
        }
    }
}
