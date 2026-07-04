using HomeSensors.Model.CameraSnapshots.Helpers;
using HomeSensors.Model.CameraSnapshots.Models;
using HomeSensors.Model.CameraSnapshots.Services;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.CameraSnapshots.Repositories;

public class CameraSnapshotTimelineRepository : RepositoryBase
{
    private static readonly string[] _supportedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly HomeSensorsContext _data;
    private readonly ThumbnailService _thumbnailService;

    public CameraSnapshotTimelineRepository(HomeSensorsContext data, ThumbnailService thumbnailService)
    {
        _data = data;
        _thumbnailService = thumbnailService;
    }

    /// <summary>
    /// Get timeline items for a camera, filtered by optional date range.
    /// Ensures thumbnails exist for each item (on-demand generation fallback).
    /// </summary>
    public async Task<IResult<List<CameraSnapshotTimelineItem>>> GetTimelineAsync(CameraSnapshotTimelineRequest request, string baseUrl)
    {
        var camera = await _data.CameraSnapshots
            .TagWith(GetTag())
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CameraId);

        if (camera is null)
        {
            return Result.Fail<List<CameraSnapshotTimelineItem>>(new Failure("Camera not found."));
        }

        if (!Directory.Exists(camera.SnapshotsPath))
        {
            return Result.Fail<List<CameraSnapshotTimelineItem>>(new Failure($"Snapshots folder not found: {camera.SnapshotsPath}"));
        }

        var files = Directory.EnumerateFiles(camera.SnapshotsPath)
            .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => Path.GetFileName(f))
            .ToList();

        var items = new List<CameraSnapshotTimelineItem>();

        foreach (var fileName in files)
        {
            var timestamp = CameraSnapshotHelpers.ParseTimestamp(fileName);

            if (timestamp is null)
            {
                continue;
            }

            if (request.Start is not null && timestamp < request.Start)
            {
                continue;
            }

            if (request.End is not null && timestamp > request.End)
            {
                continue;
            }

            // On-demand fallback: ensure thumbnails exist before returning URLs
            await _thumbnailService.EnsureThumbnailsAsync(camera, fileName);

            var cameraId = camera.Id;
            var smallUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=small";
            var mediumUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=medium";
            var largeUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=large";
            var originalUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/original/{Uri.EscapeDataString(fileName)}";

            items.Add(new CameraSnapshotTimelineItem(fileName, timestamp.Value, smallUrl, mediumUrl, largeUrl, originalUrl));
        }

        // Sort newest first
        items.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

        return Result.Ok(items);
    }
}
