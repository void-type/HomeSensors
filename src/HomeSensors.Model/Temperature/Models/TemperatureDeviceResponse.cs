namespace HomeSensors.Model.Temperature.Models;

public class TemperatureDeviceResponse
{
    public TemperatureDeviceResponse(
        long id, string name, string mqttTopic,
        long? locationId, TemperatureReadingResponse? lastReading,
        bool isRetired, bool isInactive, bool isBatteryLevelLow
        )
    {
        Id = id;
        Name = name;
        MqttTopic = mqttTopic;
        LocationId = locationId;
        LastReading = lastReading;
        IsRetired = isRetired;
        IsInactive = isInactive;
        IsBatteryLevelLow = isBatteryLevelLow;
    }

    public long Id { get; }

    public string Name { get; }

    public string MqttTopic { get; }

    public long? LocationId { get; }

    public TemperatureReadingResponse? LastReading { get; }

    public bool IsRetired { get; }

    public bool IsLost { get; }

    public bool IsInactive { get; }

    public bool IsBatteryLevelLow { get; }
}
