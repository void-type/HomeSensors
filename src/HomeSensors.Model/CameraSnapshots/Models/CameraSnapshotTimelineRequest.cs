namespace HomeSensors.Model.CameraSnapshots.Models;

public class CameraSnapshotTimelineRequest
{
    public long CameraId { get; init; }

    public DateTimeOffset? Start { get; init; }

    public DateTimeOffset? End { get; init; }
}
