namespace HomeSensors.Model.WaterLeak.Models;

public class WaterLeakDeviceResponse
{
    public WaterLeakDeviceResponse(long id, string name, string mqttTopic)
    {
        Id = id;
        Name = name;
        MqttTopic = mqttTopic;
    }

    public long Id { get; }

    public string Name { get; }

    public string MqttTopic { get; }
}
