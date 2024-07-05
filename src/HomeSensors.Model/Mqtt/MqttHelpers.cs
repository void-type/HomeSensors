using HomeSensors.Model.Helpers;
using HomeSensors.Model.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System.Text;
using System.Text.Json;
using VoidCore.Model.Guards;

namespace HomeSensors.Model.Mqtt;

public static partial class MqttHelpers
{
    private static readonly JsonSerializerOptions _serializerOptions = JsonHelpers.GetOptions();

    public static List<MqttTopicFilter> GetTopicFilters(this MqttFactory mqttFactory, IEnumerable<string> topics)
    {
        return topics
            .Select(x => mqttFactory
                .CreateTopicFilterBuilder()
                .WithTopic(x)
                .Build())
            .ToList();
    }

    public static ManagedMqttClientOptions GetClientOptions(this MqttSettings mqttSettings)
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

    public static string GetPayloadString(this MqttApplicationMessageReceivedEventArgs e)
    {
        return Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
    }

    public static MqttTemperatureMessage DeserializeTemperatureMessage(this MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = GetPayloadString(e);

        return JsonSerializer.Deserialize<MqttTemperatureMessage>(payload, _serializerOptions)
            .EnsureNotNull();
    }

    public static string GetReadableTemperatureMessage(this MqttTemperatureMessage message)
    {
        return $"{message.Time.ToLocalTime()} | {message.Model}/{message.Id}/{message.Channel} | {TemperatureHelpers.FormatTemp(message.Temperature_C ?? -1000)} | {message.Humidity}%";
    }

    public static MqttWaterLeakMessagePayload DeserializeWaterLeakMessage(this MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = GetPayloadString(e);

        return JsonSerializer.Deserialize<MqttWaterLeakMessagePayload>(payload, _serializerOptions)
            .EnsureNotNull();
    }

    public static string GetReadableWaterLeakMessage(this MqttWaterLeakMessage message)
    {
        return $"Location: {message.LocationName} | Water leak: {message.Payload.Water_Leak} | Battery low: {message.Payload.Battery_Low} | Battery: {message.Payload.Battery}%";
    }

    [LoggerMessage(0, LogLevel.Debug, "MqttMessage: {Payload}")]
    public static partial void LogMqttPayload(ILogger logger, string payload);
}
