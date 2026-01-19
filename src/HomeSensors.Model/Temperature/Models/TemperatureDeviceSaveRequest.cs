namespace HomeSensors.Model.Temperature.Models;

public class TemperatureDeviceSaveRequest
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string MqttTopic { get; init; } = string.Empty;

    public long? LocationId { get; init; }

    public bool IsRetired { get; init; }
}
