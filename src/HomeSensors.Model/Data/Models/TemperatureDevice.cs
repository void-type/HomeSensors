namespace HomeSensors.Model.Data.Models;

public class TemperatureDevice
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MqttTopic { get; set; } = string.Empty;

    public string DeviceModel { get; init; } = string.Empty;
    public string? DeviceId { get; init; } = string.Empty;
    public string? DeviceChannel { get; init; } = string.Empty;

    public bool IsRetired { get; set; }

    public long? TemperatureLocationId { get; set; }
    public virtual TemperatureLocation? TemperatureLocation { get; set; }

    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = [];
}
