using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

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
        using var mqttClient = _mqttFactory.CreateMqttClient();

        _logger.LogInformation("Subscribing MQTT client.");

        var mqttClientOptionBuilder = new MqttClientOptionsBuilder();
        mqttClientOptionBuilder.WithTcpServer(_configuration.Server, _configuration.Port);

        if (!string.IsNullOrWhiteSpace(_configuration.Username))
        {
            mqttClientOptionBuilder.WithCredentials(_configuration.Username, _configuration.Password);
        }

        var mqttClientOptions = mqttClientOptionBuilder.Build();

        // Setup message handling before connecting so that queued messages
        // are also handled properly. When there is no event handler attached all
        // received messages get lost.
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            ProcessMessage(e);
            return Task.CompletedTask;
        };

        var mqttSubscribeOptions = _mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => { f.WithTopic("rtl_433/Ambientweather-F007TH/96"); })
            .WithTopicFilter(f => { f.WithTopic("rtl_433/Ambientweather-F007TH/9"); })
            .WithTopicFilter(f => { f.WithTopic("rtl_433/Acurite-986/1369"); })
            .WithTopicFilter(f => { f.WithTopic("rtl_433/Acurite-986/1254"); })
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);
        await mqttClient.SubscribeAsync(mqttSubscribeOptions, stoppingToken);

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

            _logger.LogInformation("{Output}", $"{message.Model}({message.Id}): {message.Temperature_C} C | {message.Humidity}%");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing payload.");
        }
    }
}
