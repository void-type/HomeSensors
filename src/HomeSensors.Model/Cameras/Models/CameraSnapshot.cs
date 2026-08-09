namespace HomeSensors.Model.Cameras.Models;

public class CameraSnapshot
{
    public CameraSnapshot(string fileName, DateTimeOffset timestamp, string smallUrl, string mediumUrl, string originalUrl)
    {
        FileName = fileName;
        Timestamp = timestamp;
        SmallUrl = smallUrl;
        MediumUrl = mediumUrl;
        OriginalUrl = originalUrl;
    }

    public string FileName { get; }

    public DateTimeOffset Timestamp { get; }

    public string SmallUrl { get; }

    public string MediumUrl { get; }

    public string OriginalUrl { get; }
}
