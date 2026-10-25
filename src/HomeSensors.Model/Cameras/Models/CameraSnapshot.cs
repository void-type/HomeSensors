namespace HomeSensors.Model.Cameras.Models;

public class CameraSnapshot(string fileName, DateTimeOffset timestamp, string smallUrl, string mediumUrl, string originalUrl)
{
    public string FileName { get; } = fileName;

    public DateTimeOffset Timestamp { get; } = timestamp;

    public string SmallUrl { get; } = smallUrl;

    public string MediumUrl { get; } = mediumUrl;

    public string OriginalUrl { get; } = originalUrl;
}
