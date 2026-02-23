namespace HomeSensors.Model.WaterLeak.Models;

public record MqttWaterLeakDeviceMessage(string LocationName, MqttWaterLeakDeviceMessagePayload Payload, int InactiveLimitMinutes);
