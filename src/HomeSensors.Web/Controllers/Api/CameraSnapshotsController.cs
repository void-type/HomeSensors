using HomeSensors.Model.Cameras.Models;
using HomeSensors.Model.Cameras.Repositories;
using HomeSensors.Model.Cameras.Services;
using HomeSensors.Model.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Configuration;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/camera-snapshots")]
public class CameraSnapshotsController : ControllerBase
{
    private readonly CameraSnapshotRepository _cameraSnapshotRepository;
    private readonly ThumbnailService _thumbnailService;
    private readonly HomeSensorsContext _data;
    private readonly WebApplicationSettings _applicationSettings;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public CameraSnapshotsController(CameraSnapshotRepository cameraSnapshotRepository, ThumbnailService thumbnailService, HomeSensorsContext data, WebApplicationSettings applicationSettings)
    {
        _cameraSnapshotRepository = cameraSnapshotRepository;
        _thumbnailService = thumbnailService;
        _data = data;
        _applicationSettings = applicationSettings;
    }

    [HttpGet]
    [Route("{cameraId}/timeline")]
    [ProducesResponseType(typeof(List<CameraSnapshot>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimelineAsync(long cameraId, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken)
    {
        var baseUrl = _applicationSettings.BaseUrl
            .DefaultIfNullOrWhiteSpace($"{Request.Scheme}://{Request.Host}");

        var request = new CameraSnapshotTimelineRequest
        {
            CameraId = cameraId,
            Start = start,
            End = end,
        };

        return await _cameraSnapshotRepository.GetTimelineAsync(request, baseUrl, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpGet]
    [Route("{cameraId}/thumbnail/{fileName}")]
    public async Task<IActionResult> GetThumbnailAsync(long cameraId, string fileName, [FromQuery] string size = "medium", CancellationToken cancellationToken = default)
    {
        var camera = await _data.Cameras.FirstOrDefaultAsync(x => x.Id == cameraId, cancellationToken);

        if (camera is null)
        {
            return NotFound("Camera not found.");
        }

        var thumbnailSize = size switch
        {
            "small" => ThumbnailSize.Small,
            _ => ThumbnailSize.Medium,
        };

        await _thumbnailService.EnsureThumbnailsAsync(camera, fileName, cancellationToken);

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
        var camera = await _data.Cameras.FirstOrDefaultAsync(x => x.Id == cameraId);

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

    [HttpPost]
    [Route("{cameraId}/upload")]
    [ProducesResponseType(typeof(EntityMessage<string>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 404)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 409)]
    public async Task<IActionResult> UploadOriginalAsync(long cameraId, IFormFile file, [FromForm] string timestamp, CancellationToken cancellationToken)
    {
        await using var uploadStream = file.OpenReadStream();

        var request = new CameraSnapshotUploadRequest
        {
            CameraId = cameraId,
            Timestamp = timestamp,
            FileName = file.FileName,
            FileContent = uploadStream,
        };

        return await _cameraSnapshotRepository.UploadAsync(request, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }
}
