namespace HomeSensors.Model.Temperature.Models;

public class TemperatureDeviceResponse(
    long id, string name, string mqttTopic,
    long? locationId, TemperatureReadingResponse? lastReading,
    bool isRetired, bool isInactive, bool isBatteryLevelLow,
    bool excludeFromInactiveAlerts, int inactiveLimitMinutes
        )
{
    public long Id { get; } = id;

    public string Name { get; } = name;

    public string MqttTopic { get; } = mqttTopic;

    public long? LocationId { get; } = locationId;

    public TemperatureReadingResponse? LastReading { get; } = lastReading;

    public bool IsRetired { get; } = isRetired;

    public bool IsLost { get; }

    public bool IsInactive { get; } = isInactive;

    public bool IsBatteryLevelLow { get; } = isBatteryLevelLow;

    public bool ExcludeFromInactiveAlerts { get; } = excludeFromInactiveAlerts;

    public int InactiveLimitMinutes { get; } = inactiveLimitMinutes;
}
