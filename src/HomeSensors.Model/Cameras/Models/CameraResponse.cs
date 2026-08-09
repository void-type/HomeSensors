namespace HomeSensors.Model.Cameras.Models;

public class CameraResponse
{
    public CameraResponse(long id, string name, string snapshotsPath, string slug, bool isHidden)
    {
        Id = id;
        Name = name;
        SnapshotsPath = snapshotsPath;
        Slug = slug;
        IsHidden = isHidden;
    }

    public long Id { get; }

    public string Name { get; }

    public string SnapshotsPath { get; }

    public string Slug { get; }

    public bool IsHidden { get; }
}
