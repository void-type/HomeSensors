using HomeSensors.Model.CameraSnapshots.Models;
using HomeSensors.Model.CameraSnapshots.Repositories;
using HomeSensors.Model.CameraSnapshots.Services;
using HomeSensors.Model.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Text;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/camera-snapshot-timeline")]
public class CameraSnapshotTimelineController : ControllerBase
{
    private readonly CameraSnapshotTimelineRepository _timelineRepository;
    private readonly ThumbnailService _thumbnailService;
    private readonly HomeSensorsContext _data;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public CameraSnapshotTimelineController(CameraSnapshotTimelineRepository timelineRepository, ThumbnailService thumbnailService, HomeSensorsContext data)
    {
        _timelineRepository = timelineRepository;
        _thumbnailService = thumbnailService;
        _data = data;
    }

    [HttpGet]
    [Route("{cameraId}/items")]
    [ProducesResponseType(typeof(List<CameraSnapshotTimelineItem>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetItemsAsync(long cameraId, DateTimeOffset? start, DateTimeOffset? end)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var request = new CameraSnapshotTimelineRequest
        {
            CameraId = cameraId,
            Start = start,
            End = end,
        };

        return await _timelineRepository.GetTimelineAsync(request, baseUrl)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpGet]
    [Route("{cameraId}/thumbnail/{fileName}")]
    public async Task<IActionResult> GetThumbnailAsync(long cameraId, string fileName, [FromQuery] string size = "medium")
    {
        var camera = await _data.CameraSnapshots.FirstOrDefaultAsync(x => x.Id == cameraId);

        if (camera is null)
        {
            return NotFound("Camera not found.");
        }

        var thumbnailSize = size switch
        {
            "small" => ThumbnailSize.Small,
            _ => ThumbnailSize.Medium,
        };

        // Ensure thumbnail exists (on-demand generation)
        await _thumbnailService.EnsureThumbnailsAsync(camera, fileName);

        var thumbnailPath = _thumbnailService.GetThumbnailPath(camera, fileName, thumbnailSize);

        if (!System.IO.File.Exists(thumbnailPath))
        {
            return NotFound("Thumbnail not found.");
        }

        Response.Headers.CacheControl = "public, max-age=3600";
        return PhysicalFile(thumbnailPath, "image/webp");
    }

    [HttpGet]
    [Route("{cameraId}/original/{fileName}")]
    public async Task<IActionResult> GetOriginalAsync(long cameraId, string fileName)
    {
        var camera = await _data.CameraSnapshots.FirstOrDefaultAsync(x => x.Id == cameraId);

        if (camera is null)
        {
            return NotFound("Camera not found.");
        }

        var originalPath = Path.Combine(camera.SnapshotsPath, TextHelpers.GetSafeFileName(fileName, "_"));

        if (!System.IO.File.Exists(originalPath))
        {
            return NotFound("Snapshot not found.");
        }

        var contentType = _contentTypeProvider.TryGetContentType(fileName, out var detected)
            ? detected
            : "application/octet-stream";

        Response.Headers.CacheControl = "public, max-age=3600";
        return PhysicalFile(originalPath, contentType);
    }
}
