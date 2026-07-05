using HomeSensors.Model.CameraSnapshots.Helpers;
using HomeSensors.Model.CameraSnapshots.Models;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.CameraSnapshots.Repositories;

public class CameraSnapshotTimelineRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public CameraSnapshotTimelineRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    /// <summary>
    /// Get timeline items for a camera, filtered by optional date range.
    /// Thumbnails are generated on-demand by the thumbnail endpoint or in the background by the worker.
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

        if (request.Start is null || request.End is null)
        {
            return Result.Fail<List<CameraSnapshotTimelineItem>>(new Failure("Start and end dates are required."));
        }

        if (request.End <= request.Start)
        {
            return Result.Fail<List<CameraSnapshotTimelineItem>>(new Failure("End date must be after start date."));
        }

        if (request.End - request.Start > TimeSpan.FromDays(182))
        {
            return Result.Fail<List<CameraSnapshotTimelineItem>>(new Failure("Date range cannot exceed 6 months."));
        }

        if (!Directory.Exists(camera.SnapshotsPath))
        {
            return Result.Fail<List<CameraSnapshotTimelineItem>>(new Failure($"Snapshots folder not found: {camera.SnapshotsPath}"));
        }

        var files = CameraSnapshotHelpers.GetSnapshotFileNames(camera.SnapshotsPath).ToList();

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

            var cameraId = camera.Id;
            var smallUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=small";
            var mediumUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=medium";
            var largeUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=large";
            var originalUrl = $"{baseUrl}/api/camera-snapshot-timeline/{cameraId}/original/{Uri.EscapeDataString(fileName)}";

            items.Add(new CameraSnapshotTimelineItem(fileName, timestamp.Value, smallUrl, mediumUrl, largeUrl, originalUrl));
        }

        // Sort oldest first (left = past, right = present)
        items.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return Result.Ok(items);
    }
}
