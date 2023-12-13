namespace HomeSensors.Model.Workers;

public class MqttWaterLeaksSettings
{
    public bool IsEnabled { get; set; } = true;
    public int BetweenNotificationsMinutes { get; set; } = 120;
    public MqttWaterLeakSensor[] Sensors { get; set; } = [];
}

public record MqttWaterLeakSensor(string Name, string Topic);
