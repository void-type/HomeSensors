namespace HomeSensors.Model.TemperatureRepositories.Models;

public class Reading
{
    public long Id { get; init; }
    public DateTimeOffset Time { get; init; }
    public double? DeviceBatteryLevel { get; init; }
    public int? DeviceStatus { get; init; }
    public double? Humidity { get; init; }
    public double? TemperatureCelsius { get; init; }
    public long TemperatureDeviceId { get; init; }
    public long? TemperatureLocationId { get; init; }
}
