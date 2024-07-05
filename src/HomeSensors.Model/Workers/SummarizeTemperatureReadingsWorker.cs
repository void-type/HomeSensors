using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Workers;

/// <summary>
/// This worker compresses older data by summarizing into regular average intervals.
/// </summary>
public class SummarizeTemperatureReadingsWorker : BackgroundService
{
    private readonly ILogger<SummarizeTemperatureReadingsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _betweenTicks;
    private readonly TimeSpan _summarizeCutoff;
    private const int SummarizeIntervalMinutes = 5;
    private readonly SummarizeTemperatureReadingsSettings _workerSettings;
    private readonly AlertsSettings _alertsSettings;

    public SummarizeTemperatureReadingsWorker(ILogger<SummarizeTemperatureReadingsWorker> logger, IServiceScopeFactory scopeFactory,
        IDateTimeService dateTimeService, SummarizeTemperatureReadingsSettings workerSettings, AlertsSettings alertsSettings)
    {
        _workerSettings = workerSettings;
        _alertsSettings = alertsSettings;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);
        _summarizeCutoff = TimeSpan.FromDays(workerSettings.SummarizeCutoffDays);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Offset the schedule of this job from others
        if (_alertsSettings.IsEnabled)
        {
            await Task.Delay(TimeSpan.FromMinutes(_workerSettings.DelayFirstTickMinutes), stoppingToken);
        }

        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(SummarizeTemperatureReadingsWorker));

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

                var cutoffLimit = _dateTimeService.MomentWithOffset
                    .Subtract(_summarizeCutoff)
                    .RoundDownMinutes(SummarizeIntervalMinutes);

                var devices = await dbContext.TemperatureDevices
                    .TagWith($"Query called from {nameof(SummarizeTemperatureReadingsWorker)}.")
                    .ToListAsync(stoppingToken);

                var deletedCount = 0;
                var createdCount = 0;

                foreach (var device in devices)
                {
                    try
                    {
                        var oldReadings = await dbContext.TemperatureReadings
                            .TagWith($"Query called from {nameof(SummarizeTemperatureReadingsWorker)}.")
                            .AsNoTracking()
                            .Where(x => !x.IsSummary && x.Time < cutoffLimit && x.TemperatureDeviceId == device.Id)
                            .ToListAsync(stoppingToken);

                        if (oldReadings.Count == 0)
                        {
                            continue;
                        }

                        var newReadings = oldReadings
                            .GroupBy(x => (deviceId: x.TemperatureDeviceId, locationId: x.TemperatureLocationId, intervalTime: x.Time.RoundDownMinutes(SummarizeIntervalMinutes)))
                            .Select(group => new
                            {
                                OldReadings = group.ToArray(),
                                NewReading = new TemperatureReading()
                                {
                                    Time = group.Key.intervalTime,
                                    DeviceBatteryLevel = null,
                                    DeviceStatus = null,
                                    Humidity = group.Average(x => x.Humidity),
                                    TemperatureCelsius = group.Average(x => x.TemperatureCelsius),
                                    IsSummary = true,
                                    TemperatureDeviceId = group.Key.deviceId,
                                    TemperatureLocationId = group.Key.locationId
                                }
                            })
                            .ToList();

                        // This takes more time, but may prevent deadlocks with other jobs.
                        // EF uses a MERGE statement that seems to conflict with reads on the same table when it take a long time.
                        foreach (var newReadingChunk in newReadings.Chunk(_workerSettings.ChunkSize))
                        {
                            var readingsToCreate = newReadingChunk
                                .Select(x => x.NewReading)
                                .ToArray();

                            var readingsToDelete = newReadingChunk
                                .SelectMany(x => x.OldReadings)
                                .ToArray();

                            dbContext.TemperatureReadings.AddRange(readingsToCreate);
                            dbContext.TemperatureReadings.RemoveRange(readingsToDelete);

                            await dbContext.SaveChangesAsync(stoppingToken);

                            createdCount += readingsToCreate.Length;
                            deletedCount += readingsToDelete.Length;

                            if (stoppingToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception thrown in {WorkerName} while processing device {Name} {MqTtTopic}.", nameof(SummarizeTemperatureReadingsWorker), device.Name, device.MqttTopic);
                    }

                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                _logger.LogInformation("Summarized data older than {Cutoff}. Deleted {DeletedCount} rows and added {CreatedCount} rows.", cutoffLimit.ToString("o"), deletedCount, createdCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(SummarizeTemperatureReadingsWorker));
            }

            _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(SummarizeTemperatureReadingsWorker), Stopwatch.GetElapsedTime(startTime));
        }
    }
}
