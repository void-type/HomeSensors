namespace HomeSensors.Model.Repositories.Models;

public class LostDevice
{
    public LostDevice(long id, string? deviceModel, string? deviceId, string? deviceChannel, double? lastReadingTemperatureCelsius, DateTimeOffset? lastReadingTime)
    {
        Id = id;
        DeviceModel = deviceModel;
        DeviceId = deviceId;
        DeviceChannel = deviceChannel;
        LastReadingTemperatureCelsius = lastReadingTemperatureCelsius;
        LastReadingTime = lastReadingTime;
    }

    public long Id { get; }
    public string? DeviceModel { get; }
    public string? DeviceId { get; }
    public string? DeviceChannel { get; }
    public double? LastReadingTemperatureCelsius { get; }
    public DateTimeOffset? LastReadingTime { get; }
}
