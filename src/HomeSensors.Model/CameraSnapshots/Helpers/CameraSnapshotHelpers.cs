using HomeSensors.Model.CameraSnapshots.Entities;
using HomeSensors.Model.CameraSnapshots.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HomeSensors.Model.CameraSnapshots.Helpers;

public static partial class CameraSnapshotHelpers
{
    private static readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.Local;

    // Matches timestamp pattern yyyyMMdd_HHmmss anywhere in the filename
    [GeneratedRegex(@"(\d{8}_\d{6})")]
    private static partial Regex TimestampRegex();

    /// <summary>
    /// Supported image file extensions for camera snapshots.
    /// </summary>
    public static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    /// <summary>
    /// Enumerate all supported snapshot file names from the given directory.
    /// </summary>
    public static IEnumerable<string> GetSnapshotFileNames(string snapshotsPath) =>
        Directory.EnumerateFiles(snapshotsPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(Path.GetFileName)
            .OfType<string>();

    /// <summary>
    /// Parse the timestamp from a snapshot filename.
    /// Expected pattern: ..._yyyyMMdd_HHmmss anywhere in the filename.
    /// Returns null if no timestamp found.
    /// </summary>
    public static DateTimeOffset? ParseTimestamp(string fileName)
    {
        var match = TimestampRegex().Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        if (!DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return null;
        }

        return new DateTimeOffset(parsed, _localTimeZone.GetUtcOffset(parsed));
    }

    /// <summary>
    /// Convert a camera name to a URL/path-safe slug (e.g. "Back Yard" → "back-yard").
    /// </summary>
    public static string ToSlug(string name)
    {
        var lower = name.ToLowerInvariant();
        var slug = SlugCleanRegex().Replace(lower, "-");
        return MultiDashRegex().Replace(slug, "-").Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugCleanRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultiDashRegex();

    /// <summary>
    /// Convert entity to API response DTO.
    /// </summary>
    public static CameraSnapshotResponse ToApiResponse(this CameraSnapshot entity) =>
        new(entity.Id, entity.Name, entity.SnapshotsPath, entity.IsHidden);
}
