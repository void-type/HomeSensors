using HomeSensors.Model.Cameras.Helpers;
using HomeSensors.Model.Cameras.Models;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;

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

    /// <summary>
    /// Upload a new original snapshot file for a camera. Thumbnails are not generated during upload.
    /// Returns a failure if the camera is not found, the file extension is unsupported, the timestamp
    /// is invalid, or a file for the same camera and timestamp already exists.
    /// </summary>
    public async Task<IResult<EntityMessage<string>>> UploadAsync(CameraSnapshotUploadRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<IFailure>();

        var camera = await _data.Cameras
            .TagWith(GetTag())
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CameraId, cancellationToken);

        if (camera is null)
        {
            return Result.Fail<EntityMessage<string>>(new Failure("Camera not found.", "cameraId"));
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();

        if (!CameraHelpers.SupportedExtensions.Contains(extension))
        {
            failures.Add(new Failure($"Unsupported file extension '{extension}'. Supported: {string.Join(", ", CameraHelpers.SupportedExtensions)}", "file"));
        }

        if (!DateTimeOffset.TryParse(request.Timestamp, CultureInfo.InvariantCulture, out var parsedTimestamp))
        {
            failures.Add(new Failure($"Invalid timestamp '{request.Timestamp}'. Expected a parseable date/time string.", "timestamp"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<string>>(failures);
        }

        // Format using the verbatim date/time components from the parsed value — no timezone conversion.
        var timestampSegment = parsedTimestamp.ToString("yyyyMMdd_HHmmss");

        if (!Directory.Exists(camera.SnapshotsPath))
        {
            Directory.CreateDirectory(camera.SnapshotsPath);
        }

        var slug = camera.SelectSlug();
        var fileName = $"{slug}_{timestampSegment}{extension}";
        var destPath = Path.Combine(camera.SnapshotsPath, fileName);

        try
        {
            await using var stream = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await request.FileContent.CopyToAsync(stream, cancellationToken);
        }
        catch (IOException)
        {
            return Result.Fail<EntityMessage<string>>(new Failure($"A snapshot for this camera at timestamp '{timestampSegment}' already exists.", "timestamp"));
        }

        return Result.Ok(EntityMessage.Create("Snapshot uploaded.", fileName));
    }
}
