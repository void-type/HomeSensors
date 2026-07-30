using HomeSensors.Model.CameraSnapshots.Entities;
using HomeSensors.Model.CameraSnapshots.Helpers;
using HomeSensors.Model.CameraSnapshots.Settings;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace HomeSensors.Model.CameraSnapshots.Services;

public class ThumbnailService
{
    // Height-based sizes to accommodate variable aspect ratios (16:9, 1:1, 32:9, etc.)
    private const int SmallHeightPx = 180;
    private const int MediumHeightPx = 720;

    private readonly CameraSnapshotSettings _settings;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(CameraSnapshotSettings settings, ILogger<ThumbnailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Ensure all thumbnail sizes exist for the given snapshot. Skips any that already exist.
    /// </summary>
    public async Task EnsureThumbnailsAsync(CameraSnapshot camera, string fileName, CancellationToken cancellationToken = default)
    {
        var originalPath = Path.Combine(camera.SnapshotsPath, Path.GetFileName(fileName));

        if (!File.Exists(originalPath))
        {
            return;
        }

        var slug = CameraSnapshotHelpers.ToSlug(camera.Name);
        var cacheDir = Path.Combine(_settings.ThumbnailCachePath, slug);

        Directory.CreateDirectory(cacheDir);

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var smallPath = Path.Combine(cacheDir, $"{baseName}_thumb-small.webp");
        var mediumPath = Path.Combine(cacheDir, $"{baseName}_thumb-medium.webp");

        var needsSmall = !File.Exists(smallPath);
        var needsMedium = !File.Exists(mediumPath);

        if (!needsSmall && !needsMedium)
        {
            return;
        }

        try
        {
            using var image = new MagickImage(originalPath);

            if (needsSmall)
            {
                await GenerateThumbnailAsync(image, smallPath, SmallHeightPx, isLossless: false, quality: 75, cancellationToken);
            }

            if (needsMedium)
            {
                await GenerateThumbnailAsync(image, mediumPath, MediumHeightPx, isLossless: false, quality: 80, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnails for {OriginalPath}.", originalPath);
        }
    }

    /// <summary>
    /// Get the filesystem path for a given thumbnail size.
    /// Returns null if the camera's snapshot path does not contain the file.
    /// </summary>
    public string GetThumbnailPath(CameraSnapshot camera, string fileName, ThumbnailSize size)
    {
        var slug = CameraSnapshotHelpers.ToSlug(camera.Name);
        var cacheDir = Path.Combine(_settings.ThumbnailCachePath, slug);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        return size switch
        {
            ThumbnailSize.Small => Path.Combine(cacheDir, $"{baseName}_thumb-small.webp"),
            ThumbnailSize.Medium => Path.Combine(cacheDir, $"{baseName}_thumb-medium.webp"),
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
    }

    private static async Task GenerateThumbnailAsync(MagickImage sourceImage, string outputPath, int heightPx, bool isLossless, int quality, CancellationToken cancellationToken)
    {
        // Clone so we don't modify the shared source image
        using var thumb = sourceImage.Clone() as MagickImage ?? throw new InvalidOperationException("Clone failed.");

        // Resize preserving aspect ratio, constrained by height
        thumb.Resize(new MagickGeometry { Height = (uint)heightPx, IgnoreAspectRatio = false });

        thumb.Format = MagickFormat.WebP;
        thumb.Quality = (uint)quality;

        if (isLossless)
        {
            thumb.Settings.SetDefine("webp:lossless", "true");
        }

        var tmpPath = outputPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await thumb.WriteAsync(tmpPath, MagickFormat.WebP, cancellationToken);
            File.Move(tmpPath, outputPath, overwrite: true);
        }
        catch
        {
            File.Delete(tmpPath);
            throw;
        }
    }
}

public enum ThumbnailSize
{
    Small,
    Medium,
}
