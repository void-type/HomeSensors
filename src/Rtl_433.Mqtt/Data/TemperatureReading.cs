namespace Rtl_433.Mqtt.Data;

public class TemperatureReading
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public double? DeviceBatteryLevel { get; set; }
    public int? DeviceStatus { get; set; }
    public string? MessageIntegrityCheck { get; set; } = string.Empty;
    public double? TemperatureCelsius { get; set; }
    public double? Humidity { get; set; }

    public long TemperatureDeviceId { get; set; }
    public TemperatureDevice TemperatureDevice { get; set; } = new();

    public long? TemperatureLocationId { get; set; }
    public TemperatureLocation? TemperatureLocation { get; set; }
}
