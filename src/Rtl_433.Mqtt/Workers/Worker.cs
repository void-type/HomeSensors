using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using Rtl_433.Mqtt.Models;
using Rtl_433.Mqtt.Data;
using Microsoft.EntityFrameworkCore;

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
        var message = DeserializeMessage(e);

        LogMessage(e, message);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var device = await dbContext.TemperatureDevices
            .FirstOrDefaultAsync(x => x.DeviceModel == message.Model && x.DeviceId == message.Id && x.DeviceChannel == message.Channel);

        if (device is null)
        {
            _logger.LogWarning("Device not found: {Model}/{Id} on channel {Channel}", message.Model, message.Id, message.Channel);
            return;
        }

        // If we already have a reading for this device at this time, don't save because duplicate.
        var readingExists = dbContext.TemperatureReadings
            .Any(x => x.Time == message.Time && x.TemperatureDeviceId == device.Id);

        if (readingExists)
        {
            return;
        }

        dbContext.TemperatureReadings
            .Add(new TemperatureReading
            {
                Time = message.Time,
                DeviceBatteryLevel = message.Battery_Ok,
                DeviceStatus = message.Status,
                MessageIntegrityCheck = message.Mic,
                Humidity = message.Humidity,
                TemperatureCelsius = message.Temperature_C,
                TemperatureDevice = device,
                TemperatureLocationId = device.CurrentTemperatureLocationId
            });

        await dbContext.SaveChangesAsync();
    }

    private void LogMessage(MqttApplicationMessageReceivedEventArgs e, TemperatureMessage message)
    {
        if (!_configuration.LogMessages)
        {
            return;
        }

        _logger.LogInformation("{Output}", $"{e.ApplicationMessage.Topic} {message.Time} | {message.Model}:{message.Id} | {message.Temperature_C} C | {message.Humidity} %");
    }

    private TemperatureMessage DeserializeMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonConvert.DeserializeObject<TemperatureMessage>(payload);

            ArgumentNullException.ThrowIfNull(message);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing payload.");
            throw;
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
