namespace HomeSensors.Model.Data.Models;

public class LostDevice
{
    public long Id { get; init; }
    public string? DeviceModel { get; init; }
    public string? DeviceId { get; init; }
    public string? DeviceChannel { get; init; }
    public double? LastReadingTemperatureCelsius { get; init; }
    public DateTimeOffset? LastReadingTime { get; init; }
}
