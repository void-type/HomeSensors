namespace HomeSensors.Web.Services.MqttDiscovery;

public record MqttDiscoveryClientStatus(string[]? Topics, bool IsCreated, bool IsConnected);
