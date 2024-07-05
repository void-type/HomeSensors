namespace HomeSensors.Model.Repositories.Models;

public class Device
{
    public Device(
        long id, string name, string mqttTopic,
        long? locationId, Reading? lastReading,
        bool isRetired, bool isLost, bool isInactive, bool isBatteryLevelLow
        )
    {
        Id = id;
        Name = name;
        MqttTopic = mqttTopic;
        LocationId = locationId;
        LastReading = lastReading;
        IsRetired = isRetired;
        IsLost = isLost;
        IsInactive = isInactive;
        IsBatteryLevelLow = isBatteryLevelLow;
    }

    public long Id { get; }
    public string Name { get; }
    public string MqttTopic { get; }
    public long? LocationId { get; }
    public Reading? LastReading { get; }
    public bool IsRetired { get; }
    public bool IsLost { get; }
    public bool IsInactive { get; }
    public bool IsBatteryLevelLow { get; }
}
