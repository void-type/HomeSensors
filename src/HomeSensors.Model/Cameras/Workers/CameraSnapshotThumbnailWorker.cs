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
public class CameraSnapshotThumbnailWorker : BackgroundService
{
    private readonly ILogger<CameraSnapshotThumbnailWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _betweenTicks;

    public CameraSnapshotThumbnailWorker(ILogger<CameraSnapshotThumbnailWorker> logger, IServiceScopeFactory scopeFactory, CameraWorkerSettings workerSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(CameraSnapshotThumbnailWorker),
            workerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(CameraSnapshotThumbnailWorker));

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();
                var thumbnailService = scope.ServiceProvider.GetRequiredService<ThumbnailService>();

                var cameras = await dbContext.Cameras
                    .TagWith($"Query called from {nameof(CameraSnapshotThumbnailWorker)}.")
                    .ToListAsync(stoppingToken);

                var processedCount = 0;

                foreach (Camera camera in cameras)
                {
                    if (!Directory.Exists(camera.SnapshotsPath))
                    {
                        _logger.LogWarning("{JobName}: Snapshots path not found for camera {CameraName}: {Path}.",
                            nameof(CameraSnapshotThumbnailWorker), camera.Name, camera.SnapshotsPath);
                        continue;
                    }

                    var files = CameraHelpers.GetSnapshotFileNames(camera.SnapshotsPath).ToList();

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
                            _logger.LogError(ex, "{JobName}: Error generating thumbnails for {FileName} in camera {CameraName}.",
                                nameof(CameraSnapshotThumbnailWorker), fileName, camera.Name);
                        }
                    }

                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                _logger.LogInformation("{JobName} job processed {ProcessedCount} snapshot(s).",
                    nameof(CameraSnapshotThumbnailWorker), processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(CameraSnapshotThumbnailWorker));
            }

            _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(CameraSnapshotThumbnailWorker), Stopwatch.GetElapsedTime(startTime));
        }
    }
}
