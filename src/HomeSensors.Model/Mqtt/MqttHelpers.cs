using HomeSensors.Model.Helpers;
using HomeSensors.Model.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System.Text;
using System.Text.Json;
using VoidCore.Model.Guards;

namespace HomeSensors.Model.Mqtt;

public static class MqttHelpers
{
    private static readonly JsonSerializerOptions _serializerOptions = JsonHelpers.GetOptions();

    public static List<MqttTopicFilter> BuildTopicFilters(MqttFactory mqttFactory, string[] topics)
    {
        var topicFilters = new List<MqttTopicFilter>();

        foreach (var topic in topics)
        {
            var topicFilter = mqttFactory
                .CreateTopicFilterBuilder()
                .WithTopic(topic)
                .Build();

            topicFilters.Add(topicFilter);
        }

        return topicFilters;
    }

    public static ManagedMqttClientOptions BuildOptions(MqttSettings mqttSettings)
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttSettings.Server, mqttSettings.Port);

        if (!string.IsNullOrWhiteSpace(mqttSettings.Username))
        {
            clientOptionsBuilder.WithCredentials(mqttSettings.Username, mqttSettings.Password);
        }

        var clientOptions = clientOptionsBuilder.Build();

        return new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptions)
            .Build();
    }

    public static string GetPayloadString(MqttApplicationMessageReceivedEventArgs e)
    {
        return Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
    }

    public static MqttTemperatureMessage DeserializeTemperatureMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = GetPayloadString(e);

        return JsonSerializer.Deserialize<MqttTemperatureMessage>(payload, _serializerOptions)
            .EnsureNotNull();
    }

    public static string GetReadableTemperatureMessage(MqttTemperatureMessage message)
    {
        return $"{message.Time.ToLocalTime()} | {message.Model}/{message.Id}/{message.Channel} | {TemperatureHelpers.FormatTemp(message.Temperature_C ?? -1000)} | {message.Humidity}%";
    }

    public static MqttWaterLeakMessagePayload DeserializeWaterLeakMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = GetPayloadString(e);

        return JsonSerializer.Deserialize<MqttWaterLeakMessagePayload>(payload, _serializerOptions)
            .EnsureNotNull();
    }

    public static string GetReadableWaterLeakMessage(MqttWaterLeakMessage message)
    {
        return $"Location: {message.LocationName} | Water leak: {message.Payload.Water_Leak} | Battery low: {message.Payload.Battery_Low} | Battery: {message.Payload.Battery}%";
    }
}
