namespace Rtl_433.Mqtt.Data;

public class TemperatureReading
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string? DeviceId { get; set; } = string.Empty;
    public string? DeviceChannel { get; set; } = string.Empty;
    public double? DeviceBatteryLevel { get; set; }
    public int? DeviceStatus { get; set; }
    public string? MessageIntegrityCheck { get; set; } = string.Empty;
    public double? TemperatureCelsius { get; set; }
    public double? Humidity { get; set; }
}
