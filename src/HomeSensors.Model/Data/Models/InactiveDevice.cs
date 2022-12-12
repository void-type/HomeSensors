﻿namespace HomeSensors.Model.Data.Models;

public class InactiveDevice
{
    public long Id { get; init; }
    public string? DeviceModel { get; init; }
    public string? DeviceId { get; init; }
    public string? DeviceChannel { get; init; }
    public string? LocationName { get; init; }
    public double? LastReadingTemperatureCelsius { get; init; }
    public DateTimeOffset? LastReadingTime { get; init; }
}