namespace HomeSensors.Model.Cameras.Settings;

public class CameraWorkerSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksMinutes { get; init; } = 10;
}
