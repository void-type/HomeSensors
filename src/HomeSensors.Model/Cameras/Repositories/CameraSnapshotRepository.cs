using HomeSensors.Model.Cameras.Helpers;
using HomeSensors.Model.Cameras.Models;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Cameras.Repositories;

public class CameraSnapshotRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public CameraSnapshotRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    /// <summary>
    /// Get timeline items for a camera, filtered by optional date range.
    /// Thumbnails are generated on-demand by the thumbnail endpoint or in the background by the worker.
    /// </summary>
    public async Task<IResult<List<CameraSnapshot>>> GetTimelineAsync(CameraSnapshotTimelineRequest request, string baseUrl, CancellationToken cancellationToken)
    {
        var camera = await _data.Cameras
            .TagWith(GetTag())
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CameraId, cancellationToken);

        if (camera is null)
        {
            return Result.Fail<List<CameraSnapshot>>(new Failure("Camera not found."));
        }

        if (request.Start is null || request.End is null)
        {
            return Result.Fail<List<CameraSnapshot>>(new Failure("Start and end dates are required."));
        }

        if (request.End <= request.Start)
        {
            return Result.Fail<List<CameraSnapshot>>(new Failure("End date must be after start date."));
        }

        if (request.End - request.Start > TimeSpan.FromDays(182))
        {
            return Result.Fail<List<CameraSnapshot>>(new Failure("Date range cannot exceed 6 months."));
        }

        if (!Directory.Exists(camera.SnapshotsPath))
        {
            return Result.Fail<List<CameraSnapshot>>(new Failure($"Snapshots folder not found: {camera.SnapshotsPath}"));
        }

        var files = CameraHelpers.GetSnapshotFileNames(camera.SnapshotsPath).ToList();

        var items = new List<CameraSnapshot>();

        foreach (var fileName in files)
        {
            var timestamp = CameraHelpers.ParseTimestamp(fileName);

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
            var smallUrl = $"{baseUrl}/api/camera-snapshots/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=small";
            var mediumUrl = $"{baseUrl}/api/camera-snapshots/{cameraId}/thumbnail/{Uri.EscapeDataString(fileName)}?size=medium";
            var originalUrl = $"{baseUrl}/api/camera-snapshots/{cameraId}/original/{Uri.EscapeDataString(fileName)}";

            items.Add(new CameraSnapshot(fileName, timestamp.Value, smallUrl, mediumUrl, originalUrl));
        }

        // Sort oldest first (left = past, right = present)
        items.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return Result.Ok(items);
    }
}
