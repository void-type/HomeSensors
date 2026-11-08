using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Temperature.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Temperature.Workers;

/// <summary>
/// This worker compresses older data by summarizing into regular average intervals.
/// </summary>
public partial class SummarizeTemperatureReadingsWorker : BackgroundService
{
    private readonly ILogger<SummarizeTemperatureReadingsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _betweenTicks;
    private readonly TimeSpan _summarizeCutoff;
    private const int SummarizeIntervalMinutes = 5;
    private readonly SummarizeTemperatureReadingsSettings _workerSettings;

    public SummarizeTemperatureReadingsWorker(ILogger<SummarizeTemperatureReadingsWorker> logger, IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider, SummarizeTemperatureReadingsSettings workerSettings)
    {
        _workerSettings = workerSettings;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);
        _summarizeCutoff = TimeSpan.FromDays(workerSettings.SummarizeCutoffDays);

        EnablingBackgroundJob(logger, nameof(SummarizeTemperatureReadingsWorker), workerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Offset the schedule of this job from others
        await Task.Delay(TimeSpan.FromMinutes(_workerSettings.DelayFirstTickMinutes), stoppingToken);

        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                JobStarting(_logger, nameof(SummarizeTemperatureReadingsWorker));

                await SummarizeAllDevicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                WorkerFailed(_logger, ex, nameof(SummarizeTemperatureReadingsWorker));
            }

            var elapsed = Stopwatch.GetElapsedTime(startTime);
            JobFinished(_logger, nameof(SummarizeTemperatureReadingsWorker), elapsed);
        }
    }

    private async Task SummarizeAllDevicesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var cutoffLimit = _timeProvider.GetUtcNow()
            .Subtract(_summarizeCutoff)
            .RoundDownMinutes(SummarizeIntervalMinutes);

        var devices = await dbContext.TemperatureDevices
            .TagWith($"Query called from {nameof(SummarizeTemperatureReadingsWorker)}.")
            .ToListAsync(stoppingToken);

        var counts = new SummarizeCounts();

        foreach (var device in devices)
        {
            try
            {
                await SummarizeDeviceAsync(dbContext, device.Id, cutoffLimit, counts, stoppingToken);
            }
            catch (Exception ex)
            {
                DeviceWorkerFailed(_logger, ex, nameof(SummarizeTemperatureReadingsWorker), device.Name, device.MqttTopic);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        var cutoffText = cutoffLimit.ToString("o", CultureInfo.InvariantCulture);
        Summarized(_logger, cutoffText, counts.Deleted, counts.Created);
    }

    private async Task SummarizeDeviceAsync(HomeSensorsContext dbContext, long deviceId, DateTimeOffset cutoffLimit, SummarizeCounts counts, CancellationToken stoppingToken)
    {
        var oldReadings = await dbContext.TemperatureReadings
            .TagWith($"Query called from {nameof(SummarizeTemperatureReadingsWorker)}.")
            .AsNoTracking()
            .Where(x => !x.IsSummary && x.TemperatureDeviceId == deviceId && x.Time < cutoffLimit)
            .Select(x => new OldReading(x.Id, x.Time, x.TemperatureDeviceId, x.TemperatureLocationId, x.TemperatureCelsius, x.Humidity))
            .ToListAsync(stoppingToken);

        if (oldReadings.Count == 0)
        {
            return;
        }

        var summaries = BuildSummaries(oldReadings);

        // This takes more time, but may prevent deadlocks with other jobs.
        // EF uses a MERGE statement that seems to conflict with reads on the same table when it take a long time.
        foreach (var summaryChunk in summaries.Chunk(_workerSettings.ChunkSize))
        {
            var readingsToCreate = summaryChunk
                .Select(x => x.NewReading)
                .ToArray();

            var readingsToDelete = summaryChunk
                .SelectMany(x => x.OldReadings)
                .Select(x => new TemperatureReading()
                {
                    Id = x.Id,
                })
                .ToArray();

            await ReplaceReadingsAsync(dbContext, readingsToCreate, readingsToDelete, stoppingToken);

            counts.Created += readingsToCreate.Length;
            counts.Deleted += readingsToDelete.Length;

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static List<ReadingSummary> BuildSummaries(List<OldReading> oldReadings)
    {
        return oldReadings
            .GroupBy(x => (deviceId: x.TemperatureDeviceId, locationId: x.TemperatureLocationId, intervalTime: x.Time.RoundDownMinutes(SummarizeIntervalMinutes)))
            .Select(group => new ReadingSummary(
                group.ToArray(),
                new TemperatureReading()
                {
                    Time = group.Key.intervalTime,
                    DeviceBatteryLevel = null,
                    DeviceStatus = null,
                    Humidity = group.Average(x => x.Humidity),
                    TemperatureCelsius = group.Average(x => x.TemperatureCelsius),
                    IsSummary = true,
                    TemperatureDeviceId = group.Key.deviceId,
                    TemperatureLocationId = group.Key.locationId
                }))
            .ToList();
    }

    private static async Task ReplaceReadingsAsync(HomeSensorsContext dbContext, TemperatureReading[] readingsToCreate, TemperatureReading[] readingsToDelete, CancellationToken stoppingToken)
    {
        // SqlServerRetryingExecutionStrategy requires user-initiated transactions to be
        // wrapped in ExecuteAsync so the whole operation can be retried as a unit.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Clear tracked entities so retries start with a clean state.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, stoppingToken);

            try
            {
                await dbContext.TemperatureReadings.AddRangeAsync(readingsToCreate, stoppingToken);
                dbContext.TemperatureReadings.RemoveRange(readingsToDelete);

                await dbContext.SaveChangesAsync(stoppingToken);

                await transaction.CommitAsync(stoppingToken);
            }
            catch
            {
                await transaction.RollbackAsync(stoppingToken);
                throw;
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.")]
    private static partial void EnablingBackgroundJob(ILogger logger, string jobName, int betweenTicksMinutes);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job is starting.")]
    private static partial void JobStarting(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown in {WorkerName}.")]
    private static partial void WorkerFailed(ILogger logger, Exception exception, string workerName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job is finished in {ElapsedTime}.")]
    private static partial void JobFinished(ILogger logger, string jobName, TimeSpan elapsedTime);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown in {WorkerName} while processing device {Name} {MqTtTopic}.")]
    private static partial void DeviceWorkerFailed(ILogger logger, Exception exception, string workerName, string name, string mqTtTopic);

    [LoggerMessage(Level = LogLevel.Information, Message = "Summarized data older than {Cutoff}. Deleted {DeletedCount} rows and added {CreatedCount} rows.")]
    private static partial void Summarized(ILogger logger, string cutoff, int deletedCount, int createdCount);

    private sealed record OldReading(long Id, DateTimeOffset Time, long TemperatureDeviceId, long TemperatureLocationId, double? TemperatureCelsius, double? Humidity);

    private sealed record ReadingSummary(OldReading[] OldReadings, TemperatureReading NewReading);

    private sealed class SummarizeCounts
    {
        public int Created { get; set; }

        public int Deleted { get; set; }
    }
}
