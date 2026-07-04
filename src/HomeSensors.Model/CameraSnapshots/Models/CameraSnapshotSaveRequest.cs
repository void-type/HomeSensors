namespace HomeSensors.Model.CameraSnapshots.Models;

public class CameraSnapshotSaveRequest
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string SnapshotsPath { get; init; } = string.Empty;

    public bool IsHidden { get; init; }
}
