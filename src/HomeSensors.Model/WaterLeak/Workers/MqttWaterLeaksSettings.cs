namespace HomeSensors.Model.WaterLeak.Workers;

public class MqttWaterLeaksSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksMinutes { get; init; } = 20;
    public int BetweenNotificationsMinutes { get; init; } = 120;
    public int InactiveDeviceLimitMinutes { get; init; } = 90;
    public IEnumerable<MqttWaterLeakSettingsDevice>? Devices { get; init; } = [];
}
