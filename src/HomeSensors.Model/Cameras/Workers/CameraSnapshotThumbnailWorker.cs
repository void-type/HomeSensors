using HomeSensors.Model.Cameras.Entities;
using HomeSensors.Model.Cameras.Helpers;
using HomeSensors.Model.Cameras.Services;
using HomeSensors.Model.Cameras.Settings;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HomeSensors.Model.Cameras.Workers;

/// <summary>
/// Background worker that pre-generates thumbnails for all cameras.
/// </summary>
public partial class CameraSnapshotThumbnailWorker : BackgroundService
{
    private readonly ILogger<CameraSnapshotThumbnailWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _betweenTicks;

    public CameraSnapshotThumbnailWorker(ILogger<CameraSnapshotThumbnailWorker> logger, IServiceScopeFactory scopeFactory, CameraWorkerSettings workerSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);

        EnablingBackgroundJob(logger, nameof(CameraSnapshotThumbnailWorker), workerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                JobStarting(_logger, nameof(CameraSnapshotThumbnailWorker));

                await ProcessAllCamerasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                WorkerFailed(_logger, ex, nameof(CameraSnapshotThumbnailWorker));
            }

            var elapsed = Stopwatch.GetElapsedTime(startTime);
            JobFinished(_logger, nameof(CameraSnapshotThumbnailWorker), elapsed);
        }
    }

    private async Task ProcessAllCamerasAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();
        var thumbnailService = scope.ServiceProvider.GetRequiredService<ThumbnailService>();

        var cameras = await dbContext.Cameras
            .TagWith($"Query called from {nameof(CameraSnapshotThumbnailWorker)}.")
            .ToListAsync(stoppingToken);

        var processedCount = 0;

        foreach (var camera in cameras)
        {
            processedCount += await ProcessCameraAsync(thumbnailService, camera, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        JobProcessedSnapshots(_logger, nameof(CameraSnapshotThumbnailWorker), processedCount);
    }

    private async Task<int> ProcessCameraAsync(ThumbnailService thumbnailService, Camera camera, CancellationToken stoppingToken)
    {
        if (!Directory.Exists(camera.SnapshotsPath))
        {
            SnapshotsPathNotFound(_logger, nameof(CameraSnapshotThumbnailWorker), camera.Name, camera.SnapshotsPath);
            return 0;
        }

        var files = CameraHelpers.GetSnapshotFileNames(camera.SnapshotsPath).ToList();
        var processedCount = 0;

        foreach (var fileName in files)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await thumbnailService.EnsureThumbnailsAsync(camera, fileName, stoppingToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                ThumbnailGenerationFailed(_logger, ex, nameof(CameraSnapshotThumbnailWorker), fileName, camera.Name);
            }
        }

        return processedCount;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.")]
    private static partial void EnablingBackgroundJob(ILogger logger, string jobName, int betweenTicksMinutes);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job is starting.")]
    private static partial void JobStarting(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown in {WorkerName}.")]
    private static partial void WorkerFailed(ILogger logger, Exception exception, string workerName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job is finished in {ElapsedTime}.")]
    private static partial void JobFinished(ILogger logger, string jobName, TimeSpan elapsedTime);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job processed {ProcessedCount} snapshot(s).")]
    private static partial void JobProcessedSnapshots(ILogger logger, string jobName, int processedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{JobName}: Snapshots path not found for camera {CameraName}: {Path}.")]
    private static partial void SnapshotsPathNotFound(ILogger logger, string jobName, string cameraName, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "{JobName}: Error generating thumbnails for {FileName} in camera {CameraName}.")]
    private static partial void ThumbnailGenerationFailed(ILogger logger, Exception exception, string jobName, string fileName, string cameraName);
}
