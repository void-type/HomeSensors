namespace HomeSensors.Model.WaterLeak.Models;

public class WaterLeakDeviceResponse(long id, string name, string mqttTopic, int inactiveLimitMinutes)
{
    public long Id { get; } = id;

    public string Name { get; } = name;

    public string MqttTopic { get; } = mqttTopic;

    public int InactiveLimitMinutes { get; } = inactiveLimitMinutes;
}
