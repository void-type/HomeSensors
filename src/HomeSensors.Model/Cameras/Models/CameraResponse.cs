namespace HomeSensors.Model.Cameras.Models;

public class CameraResponse(long id, string name, string snapshotsPath, string slug, bool isHidden)
{
    public long Id { get; } = id;

    public string Name { get; } = name;

    public string SnapshotsPath { get; } = snapshotsPath;

    public string Slug { get; } = slug;

    public bool IsHidden { get; } = isHidden;
}
