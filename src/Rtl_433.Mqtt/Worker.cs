using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;

namespace Rtl_433.Mqtt;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttConfiguration _configuration;
    private readonly MqttFactory _mqttFactory;

    public Worker(ILogger<Worker> logger, MqttConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _mqttFactory = new MqttFactory();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = _mqttFactory.CreateManagedMqttClient();

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ApplicationMessageReceivedAsync += e =>
        {
            ProcessMessage(e);
            return Task.CompletedTask;
        };

        await client.StartAsync(BuildOptions());
        await client.SubscribeAsync(BuildTopicFilters());

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100000000, stoppingToken);
        }
    }

    private void ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            // If time, model, id are the same as recent, don't save because duplicate.
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonConvert.DeserializeObject<TempMessage>(payload);

            ArgumentNullException.ThrowIfNull(message);

            _logger.LogInformation("{Output}", $"{e.ApplicationMessage.Topic} {message.Time} | {message.Model}:{message.Id} | {message.Temperature_C} C | {message.Humidity} %");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing payload.");
        }
    }

    private List<MqttTopicFilter> BuildTopicFilters()
    {
        var topicFilters = new List<MqttTopicFilter>();

        foreach (var topic in _configuration.Topics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);

            var topicFilter = _mqttFactory
                .CreateTopicFilterBuilder()
                .WithTopic(topic)
                .Build();

            topicFilters.Add(topicFilter);
        }

        return topicFilters;
    }

    private ManagedMqttClientOptions BuildOptions()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_configuration.Server, _configuration.Port);

        if (!string.IsNullOrWhiteSpace(_configuration.Username))
        {
            clientOptionsBuilder.WithCredentials(_configuration.Username, _configuration.Password);
        }

        var clientOptions = clientOptionsBuilder.Build();

        var managedClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptions)
            .Build();

        return managedClientOptions;
    }
}
