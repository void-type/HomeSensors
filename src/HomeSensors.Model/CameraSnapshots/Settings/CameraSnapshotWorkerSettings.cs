namespace HomeSensors.Model.CameraSnapshots.Settings;

public class CameraSnapshotWorkerSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksMinutes { get; init; } = 10;
}
