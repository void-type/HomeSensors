using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

namespace Rtl_433.Mqtt;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttFactory _mqttFactory;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _mqttFactory = new MqttFactory();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting MQTT server.");
        // Due to security reasons the "default" endpoint (which is unencrypted) is not enabled by default!
        var mqttServerOptions = _mqttFactory.CreateServerOptionsBuilder().WithDefaultEndpoint().Build();
        using var mqttServer = _mqttFactory.CreateMqttServer(mqttServerOptions);
        await mqttServer.StartAsync();

        using var mqttClient = _mqttFactory.CreateMqttClient();

        _logger.LogInformation("Subscribing MQTT client.");

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost")
            .Build();

        // Setup message handling before connecting so that queued messages
        // are also handled properly. When there is no event handler attached all
        // received messages get lost.
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            DumpToConsole(e);

            return Task.CompletedTask;
        };

        await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);

        var mqttSubscribeOptions = _mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => { f.WithTopic("rtl_433/Acurite-986/#"); })
            .Build();

        await mqttClient.SubscribeAsync(mqttSubscribeOptions, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100000000, stoppingToken);
        }

        _logger.LogInformation("Stopping MQTT server.");
        await mqttServer.StopAsync();
    }

    private void DumpToConsole(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            // If time, model, id are the same, don't save.
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonSerializer.Deserialize<TempMessage>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("{Output}", JsonSerializer.Serialize(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing payload.");
        }
    }
}
