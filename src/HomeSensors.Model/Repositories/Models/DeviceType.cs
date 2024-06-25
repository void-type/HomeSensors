using System.Collections.Frozen;

namespace HomeSensors.Model.Repositories.Models;

public enum DeviceType
{
    TemperatureSensor,
    WaterSensor,
}

public static class DeviceTypeExtensions
{
    public static string GetDisplayValue(this DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.TemperatureSensor => "Temperature Sensor",
            DeviceType.WaterSensor => "Water sensor",
            _ => "Unknown Sensor",
        };
    }

    public static readonly FrozenDictionary<DeviceType, string> AllDeviceTypes = new Dictionary<DeviceType, string>()
        {
            { DeviceType.TemperatureSensor, DeviceType.TemperatureSensor.GetDisplayValue()},
            { DeviceType.WaterSensor, DeviceType.WaterSensor.GetDisplayValue()}
        }
        .ToFrozenDictionary();
}
