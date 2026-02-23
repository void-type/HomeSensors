namespace HomeSensors.Model.WaterLeak.Models;

public class WaterLeakDeviceResponse
{
    public WaterLeakDeviceResponse(long id, string name, string mqttTopic, int inactiveLimitMinutes)
    {
        Id = id;
        Name = name;
        MqttTopic = mqttTopic;
        InactiveLimitMinutes = inactiveLimitMinutes;
    }

    public long Id { get; }

    public string Name { get; }

    public string MqttTopic { get; }

    public int InactiveLimitMinutes { get; }
}
