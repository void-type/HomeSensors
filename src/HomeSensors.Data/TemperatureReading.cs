using Microsoft.EntityFrameworkCore;

namespace HomeSensors.Data;

[Index(nameof(Time))]
public class TemperatureReading
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public double? DeviceBatteryLevel { get; set; }
    public int? DeviceStatus { get; set; }
    public double? Humidity { get; set; }
    public double? TemperatureCelsius { get; set; }

    public long TemperatureDeviceId { get; set; }
    public TemperatureDevice TemperatureDevice { get; set; } = new();

    public long? TemperatureLocationId { get; set; }
    public TemperatureLocation? TemperatureLocation { get; set; }
}
