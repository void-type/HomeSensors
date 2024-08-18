namespace HomeSensors.Model.Services.WaterLeak;

public class MqttWaterLeaksSettings
{
    public bool IsEnabled { get; set; } = true;
    public int BetweenTicksMinutes { get; set; } = 20;
    public int BetweenNotificationsMinutes { get; set; } = 120;
    public int InactiveDeviceLimitMinutes { get; set; } = 90;
    public MqttWaterLeakSettingsDevice[] Devices { get; set; } = [];
}
