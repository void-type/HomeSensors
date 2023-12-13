namespace HomeSensors.Model.Mqtt;

public record MqttWaterLeakMessage(string LocationName, MqttWaterLeakMessagePayload Payload);
