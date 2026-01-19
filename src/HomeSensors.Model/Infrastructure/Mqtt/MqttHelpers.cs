using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System.Text;

namespace HomeSensors.Model.Infrastructure.Mqtt;

public static partial class MqttHelpers
{
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

    [LoggerMessage(0, LogLevel.Debug, "MqttMessage: {Payload}")]
    public static partial void LogMqttPayload(ILogger logger, string payload);
}
