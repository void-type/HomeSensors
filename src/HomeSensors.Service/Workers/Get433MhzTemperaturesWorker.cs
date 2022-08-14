using HomeSensors.Data;
using HomeSensors.Service.Models;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using Newtonsoft.Json;

namespace HomeSensors.Service.Workers;

public class Get433MhzTemperaturesWorker : BackgroundService
{
    private readonly ILogger<Get433MhzTemperaturesWorker> _logger;
    private readonly MqttConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;

    public Get433MhzTemperaturesWorker(ILogger<Get433MhzTemperaturesWorker> logger, MqttConfiguration configuration, IServiceScopeFactory scopeFactory)
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

        client.ApplicationMessageReceivedAsync += async e => await ProcessMessage(e);

        await client.StartAsync(BuildOptions());
        await client.SubscribeAsync(BuildTopicFilters());

        // The client is still running, we just loop here and the Delay will listen for the stop token.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100000000, stoppingToken);
        }
    }

    private async Task ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = DeserializeMessage(e);

        LogMessage(e, message);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var device = await dbContext.TemperatureDevices
            .FirstOrDefaultAsync(x => x.DeviceModel == message.Model && x.DeviceId == message.Id && x.DeviceChannel == message.Channel);

        if (device is null)
        {
            _logger.LogWarning("Device not found in database. Creating new device. ({Model}/{Id}/{Channel})", message.Model, message.Id, message.Channel);

            var savedDevice = dbContext.TemperatureDevices
            .Add(new TemperatureDevice
            {
                DeviceModel = message.Model,
                DeviceId = message.Id,
                DeviceChannel = message.Channel
            });

            await dbContext.SaveChangesAsync();

            device = savedDevice.Entity;
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
                Time = message.Time.ToLocalTime(),
                DeviceBatteryLevel = message.Battery_Ok,
                DeviceStatus = message.Status,
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

        _logger.LogInformation("{Output}", $"{e.ApplicationMessage.Topic} | {message.Time.ToLocalTime()} | {message.Model}/{message.Id}/{message.Channel} | {message.Temperature_C} C | {message.Humidity} %");
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
