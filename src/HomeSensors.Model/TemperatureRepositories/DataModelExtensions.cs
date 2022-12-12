using HomeSensors.Model.Data.Models;
using HomeSensors.Model.TemperatureRepositories.Models;

namespace HomeSensors.Model.TemperatureRepositories;

public static class DataModelExtensions
{
    public static Reading ToReading(this TemperatureReading a) => new()
    {
        Id = a.Id,
        Time = a.Time,
        DeviceBatteryLevel = a.DeviceBatteryLevel,
        DeviceStatus = a.DeviceStatus,
        Humidity = a.Humidity,
        TemperatureCelsius = a.TemperatureCelsius,
        TemperatureDeviceId = a.TemperatureDeviceId,
        TemperatureLocationId = a.TemperatureLocationId,
    };

    public static Location ToLocation(this TemperatureLocation a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        MinTemperatureLimit = a.MinTemperatureLimit,
        MaxTemperatureLimit = a.MaxTemperatureLimit
    };
}
