namespace HomeSensors.Model.Repositories.Models;

public class InactiveDevice
{
    public InactiveDevice(long id, string? deviceModel, string? deviceId, string? deviceChannel, Location? location, double? lastReadingTemperatureCelsius, DateTimeOffset? lastReadingTime)
    {
        Id = id;
        DeviceModel = deviceModel;
        DeviceId = deviceId;
        DeviceChannel = deviceChannel;
        Location = location;
        LastReadingTemperatureCelsius = lastReadingTemperatureCelsius;
        LastReadingTime = lastReadingTime;
    }

    public long Id { get; }
    public string? DeviceModel { get; }
    public string? DeviceId { get; }
    public string? DeviceChannel { get; }
    public Location? Location { get; }
    public double? LastReadingTemperatureCelsius { get; }
    public DateTimeOffset? LastReadingTime { get; }
}
