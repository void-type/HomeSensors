namespace HomeSensors.Model.Temperature.Entities;

public class TemperatureDevice
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string MqttTopic { get; set; } = string.Empty;

    public bool IsRetired { get; set; }

    public bool ExcludeFromInactiveAlerts { get; set; }

    public long? TemperatureLocationId { get; set; }

    public virtual TemperatureLocation? TemperatureLocation { get; set; }

    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = [];
}
