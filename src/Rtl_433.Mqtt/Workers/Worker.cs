using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using Rtl_433.Mqtt.Models;
using Rtl_433.Mqtt.Data;

namespace Rtl_433.Mqtt;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;

    public Worker(ILogger<Worker> logger, MqttConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
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

    private async void ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            // If time, model, id are the same as recent, don't save because duplicate.
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonConvert.DeserializeObject<TemperatureMessage>(payload);

            ArgumentNullException.ThrowIfNull(message);

            var topic = e.ApplicationMessage.Topic switch
            {
                "rtl_433/Ambientweather-F007TH/96" => "Garage",
                "rtl_433/Ambientweather-F007TH/9" => "Bedroom",
                "rtl_433/Acurite-986/1369" => "Freezer",
                "rtl_433/Acurite-986/1254" => "Fridge",
                _ => "Unknown"
            };

            // TODO: round these numbers
            var faren = (message.Temperature_C * (9f / 5f)) + 32;

            // _logger.LogInformation("{Output}", $"{e.ApplicationMessage.Topic} {message.Time} | {message.Model}:{message.Id} | {message.Temperature_C} C | {message.Humidity} %");
            _logger.LogInformation("{Output}", $"{topic} | {message.Temperature_C:0.00} C | {faren:0.00} F | {message.Humidity} % | {message.Time} | {message.Model}:{message.Id}");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

            var exists = dbContext.TemperatureReadings
                .Any(x => x.Time == message.Time && x.DeviceModel == message.Model && x.DeviceId == message.Id);

            if (exists)
            {
                return;
            }

            dbContext.TemperatureReadings.Add(new TemperatureReading
            {
                Time = message.Time,
                DeviceModel = message.Model,
                DeviceId = message.Id,
                DeviceChannel = message.Channel,
                DeviceBatteryLevel = message.Battery_Ok,
                DeviceStatus = message.Status,
                MessageIntegrityCheck = message.Mic,
                Humidity = message.Humidity,
                TemperatureCelsius = message.Temperature_C,
            });

            await dbContext.SaveChangesAsync();
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
