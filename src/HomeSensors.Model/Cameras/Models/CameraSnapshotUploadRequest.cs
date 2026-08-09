namespace HomeSensors.Model.Cameras.Models;

public class CameraSnapshotUploadRequest
{
    public long CameraId { get; set; }

    /// <summary>
    /// Verbatim timestamp string from the caller. The date/time parts are used as-is in the filename
    /// without any timezone conversion.
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public Stream FileContent { get; set; } = Stream.Null;
}
