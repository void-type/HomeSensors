namespace HomeSensors.Model.WaterLeak.Entities;

public class WaterLeakDevice
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string MqttTopic { get; set; } = string.Empty;
}
