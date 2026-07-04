namespace HomeSensors.Model.CameraSnapshots.Models;

public class CameraSnapshotResponse
{
    public CameraSnapshotResponse(long id, string name, string snapshotsPath, bool isHidden)
    {
        Id = id;
        Name = name;
        SnapshotsPath = snapshotsPath;
        IsHidden = isHidden;
    }

    public long Id { get; }

    public string Name { get; }

    public string SnapshotsPath { get; }

    public bool IsHidden { get; }
}
