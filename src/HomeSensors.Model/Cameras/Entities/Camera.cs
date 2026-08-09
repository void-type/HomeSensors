namespace HomeSensors.Model.Cameras.Entities;

public class Camera
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SnapshotsPath { get; set; } = string.Empty;

    public bool IsHidden { get; set; }
}
