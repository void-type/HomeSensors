namespace Rtl_433.Mqtt.Data;

public class TemperatureDevice
{
    public long Id { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string? DeviceId { get; set; } = string.Empty;
    public string? DeviceChannel { get; set; } = string.Empty;

    public virtual List<TemperatureDeviceLocation> TemperatureDeviceLocations { get; set; } = new();
    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = new();
}
