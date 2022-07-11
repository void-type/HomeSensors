namespace Rtl_433.Mqtt.Data;

public class TemperatureDevice
{
    public long Id { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string? DeviceId { get; set; } = string.Empty;
    public string? DeviceChannel { get; set; } = string.Empty;

    public long? CurrentTemperatureLocationId { get; set; }
    public virtual TemperatureLocation? CurrentTemperatureLocation { get; set; }

    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = new();
}
