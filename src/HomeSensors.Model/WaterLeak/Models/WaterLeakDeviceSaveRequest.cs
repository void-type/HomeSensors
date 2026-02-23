namespace HomeSensors.Model.WaterLeak.Models;

public class WaterLeakDeviceSaveRequest
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string MqttTopic { get; init; } = string.Empty;

    public int InactiveLimitMinutes { get; init; } = 90;
}
