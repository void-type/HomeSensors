﻿namespace HomeSensors.Model.Repositories.Models;

public class Device
{
    public Device(
        long id, string? deviceModel, string? deviceId, string? deviceChannel,
        long? currentLocationId, Reading? lastReading,
        bool isRetired, bool isLost, bool isInactive, bool isBatteryLevelLow
        )
    {
        Id = id;
        DeviceModel = deviceModel;
        DeviceId = deviceId;
        DeviceChannel = deviceChannel;
        CurrentLocationId = currentLocationId;
        LastReading = lastReading;
        IsRetired = isRetired;
        IsLost = isLost;
        IsInactive = isInactive;
        IsBatteryLevelLow = isBatteryLevelLow;
    }

    public long Id { get; }
    public string? DeviceModel { get; }
    public string? DeviceId { get; }
    public string? DeviceChannel { get; }
    public long? CurrentLocationId { get; }
    public Reading? LastReading { get; }
    public bool IsRetired { get; }
    public bool IsLost { get; }
    public bool IsInactive { get; }
    public bool IsBatteryLevelLow { get; }

    public string DisplayName => $"{DeviceModel}/{DeviceId}/{DeviceChannel}";
}
