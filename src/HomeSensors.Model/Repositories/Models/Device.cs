namespace HomeSensors.Model.Repositories.Models;

public class Device
{
    public Device(
        long id, string? deviceModel, string? deviceId, string? deviceChannel,
        Location? currentLocation, Reading? lastReading,
        bool isRetired, bool isLost, bool isInactive
        )
    {
        Id = id;
        DeviceModel = deviceModel;
        DeviceId = deviceId;
        DeviceChannel = deviceChannel;
        CurrentLocation = currentLocation;
        LastReading = lastReading;
        IsRetired = isRetired;
        IsLost = isLost;
        IsInactive = isInactive;
    }

    public long Id { get; }
    public string? DeviceModel { get; }
    public string? DeviceId { get; }
    public string? DeviceChannel { get; }
    public Location? CurrentLocation { get; }
    public Reading? LastReading { get; }
    public bool IsRetired { get; }
    public bool IsLost { get; }
    public bool IsInactive { get; }
}
