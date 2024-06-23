using HomeSensors.Model.Alerts;
using HomeSensors.Model.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace HomeSensors.Model.Workers;

public class MqttWaterLeaksWorker : BackgroundService
{
    private readonly ILogger<MqttWaterLeaksWorker> _logger;
    private readonly MqttSettings _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;
    private readonly MqttWaterLeaksSettings _workerSettings;

    public MqttWaterLeaksWorker(ILogger<MqttWaterLeaksWorker> logger, MqttSettings configuration, IServiceScopeFactory scopeFactory,
        MqttFactory mqttFactory, MqttWaterLeaksSettings workerSettings)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _mqttFactory = mqttFactory;
        _workerSettings = workerSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = _mqttFactory.CreateManagedMqttClient();

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectingFailedAsync += LogConnectionFailure;
        client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLogging;

        await client.StartAsync(_configuration.GetClientOptions());

        var topics = _workerSettings.Sensors.Select(x => x.Topic).ToArray();

        foreach (var topic in topics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        await client.SubscribeAsync(_mqttFactory.GetTopicFilters(topics));

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
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttWaterLeaksWorker));
        }
    }

    private async Task ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.DeserializeWaterLeakMessage();

        var name = Array.Find(_workerSettings.Sensors, x => x.Topic == e.ApplicationMessage.Topic)?.Name ??
            e.ApplicationMessage.Topic.Split("/").LastOrDefault() ??
            "Unknown";

        var message = new MqttWaterLeakMessage(name, payload);

        if (_configuration.LogMessages)
        {
            var readableMessage = message.GetReadableWaterLeakMessage();
            MqttHelpers.LogMqttPayload(_logger, readableMessage);
        }

        using var scope = _scopeFactory.CreateScope();
        var waterLeakAlertService = scope.ServiceProvider.GetRequiredService<WaterLeakAlertService>();

        await waterLeakAlertService.Process(message, _workerSettings.BetweenNotificationsMinutes);
    }
}
