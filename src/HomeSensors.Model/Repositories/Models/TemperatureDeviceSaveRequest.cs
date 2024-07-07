namespace HomeSensors.Model.Repositories.Models;

public class TemperatureDeviceSaveRequest
{
    public long Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string MqttTopic { get; set; } = string.Empty;
    public long LocationId { get; init; }
    public bool IsRetired { get; init; }
}
