using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Mqtt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace HomeSensors.Model.Workers;

public class MqttTemperaturesWorker : BackgroundService
{
    private readonly ILogger<MqttTemperaturesWorker> _logger;
    private readonly MqttSettings _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;
    private readonly WorkersSettings _workersSettings;

    public MqttTemperaturesWorker(ILogger<MqttTemperaturesWorker> logger, MqttSettings configuration, IServiceScopeFactory scopeFactory,
        MqttFactory mqttFactory, WorkersSettings workersSettings)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _mqttFactory = mqttFactory;
        _workersSettings = workersSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = _mqttFactory.CreateManagedMqttClient();

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectingFailedAsync += LogConnectionFailure;
        client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLogging;

        await client.StartAsync(MqttHelpers.BuildOptions(_configuration));

        foreach (var topic in _workersSettings.MqttTemperaturesTopics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        await client.SubscribeAsync(MqttHelpers.BuildTopicFilters(_mqttFactory, _workersSettings.MqttTemperaturesTopics));

        // The client is still running, we just loop here and the Delay will listen for the stop token.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100000000, stoppingToken);
        }
    }

    private Task LogConnectionFailure(ConnectingFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "MQTT client failed to connect.");
        return Task.CompletedTask;
    }

    private async Task ProcessMessageWithExceptionLogging(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            await ProcessMessage(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttTemperaturesWorker));
        }
    }

    private async Task ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = MqttHelpers.DeserializeMessage(e);

        var readableMessage = MqttHelpers.GetReadableMessage(message);

        if (_configuration.LogMessages)
        {
            _logger.LogInformation("{Output}", readableMessage);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var device = await dbContext.TemperatureDevices
            .TagWith($"Query called from {nameof(MqttTemperaturesWorker)}.")
            .FirstOrDefaultAsync(x => x.DeviceModel == message.Model && x.DeviceId == message.Id && x.DeviceChannel == message.Channel);

        if (device?.IsRetired == true)
        {
            return;
        }

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
        var readingExists = await dbContext.TemperatureReadings
            .TagWith($"Query called from {nameof(MqttTemperaturesWorker)}.")
            .AnyAsync(x => x.Time == message.Time && x.TemperatureDeviceId == device.Id && !x.IsSummary);

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
}
