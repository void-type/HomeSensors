using MQTTnet.Extensions.ManagedClient;

namespace HomeSensors.Web.Services.MqttDiscovery;

public record MqttDiscoveryClientState(string[] Topics, IManagedMqttClient Client);
