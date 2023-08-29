﻿using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    public SummarizeTemperatureReadingsWorker(ILogger<SummarizeTemperatureReadingsWorker> logger, IServiceScopeFactory scopeFactory,
        IDateTimeService dateTimeService, WorkersSettings workersSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _betweenTicks = TimeSpan.FromMinutes(workersSettings.SummarizeTemperatureReadingsBetweenTicksMinutes);
        _summarizeCutoff = TimeSpan.FromDays(workersSettings.SummarizeTemperatureReadingsSummarizeCutoffDays);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        // Offset the schedule of this job from others
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation($"{nameof(SummarizeTemperatureReadingsWorker)} job is starting.");

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

                var cutoffLimit = _dateTimeService.MomentWithOffset
                    .Subtract(_summarizeCutoff)
                    .RoundDownMinutes(SummarizeIntervalMinutes);

                var devices = await dbContext.TemperatureDevices
                    .TagWith($"Query called from {nameof(SummarizeTemperatureReadingsWorker)}.")
                    .ToListAsync(stoppingToken);

                var oldReadings = await dbContext.TemperatureReadings
                    .TagWith($"Query called from {nameof(SummarizeTemperatureReadingsWorker)}.")
                    .AsNoTracking()
                    .Where(x => !x.IsSummary && x.Time < cutoffLimit)
                    .ToListAsync(stoppingToken);

                if (oldReadings.Count == 0)
                {
                    return;
                }

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
                            TemperatureDevice = devices.First(x => x.Id == group.Key.device),
                            TemperatureLocationId = group.Key.location
                        }))
                    .ToList();

                dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

                // This takes more time, but may prevent deadlocks with other jobs.
                // EF uses a MERGE statement that seems to conflict with reads on the same table.
                foreach (var newReading in newReadings)
                {
                    dbContext.TemperatureReadings.Add(newReading);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                var oldReadingIds = oldReadings.Select(x => x.Id);

                await dbContext.TemperatureReadings
                    .Where(x => oldReadingIds.Contains(x.Id))
                    .ExecuteDeleteAsync(stoppingToken);

                _logger.LogInformation("Summarized data older than {Cutoff}. Compressed {OldCount} rows to {NewCount} rows.", cutoffLimit.ToString("o"), oldReadings.Count, newReadings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(SummarizeTemperatureReadingsWorker));
            }
            finally
            {
                _logger.LogInformation($"{nameof(SummarizeTemperatureReadingsWorker)} job is finished.");
            }
        }
    }
}