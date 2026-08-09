namespace HomeSensors.Model.Cameras.Models;

public class CameraSaveRequest
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string SnapshotsPath { get; init; } = string.Empty;

    public bool IsHidden { get; init; }
}
