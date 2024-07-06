namespace HomeSensors.Web.Services.MqttDiscovery;

public record MqttDiscoveryMessage(DateTimeOffset Time, string Topic, string Payload);
