namespace HomeSensors.Model.Services.WaterLeak;

public record MqttWaterLeakDeviceMessage(string LocationName, MqttWaterLeakDeviceMessagePayload Payload);
