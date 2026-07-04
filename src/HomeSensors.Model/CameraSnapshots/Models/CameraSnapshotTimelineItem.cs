namespace HomeSensors.Model.CameraSnapshots.Models;

public class CameraSnapshotTimelineItem
{
    public CameraSnapshotTimelineItem(string fileName, DateTimeOffset timestamp, string smallUrl, string mediumUrl, string largeUrl, string originalUrl)
    {
        FileName = fileName;
        Timestamp = timestamp;
        SmallUrl = smallUrl;
        MediumUrl = mediumUrl;
        LargeUrl = largeUrl;
        OriginalUrl = originalUrl;
    }

    public string FileName { get; }

    public DateTimeOffset Timestamp { get; }

    public string SmallUrl { get; }

    public string MediumUrl { get; }

    public string LargeUrl { get; }

    public string OriginalUrl { get; }
}
