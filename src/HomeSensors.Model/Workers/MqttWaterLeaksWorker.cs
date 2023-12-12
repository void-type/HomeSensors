using HomeSensors.Model.Emailing;
using HomeSensors.Model.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Workers;

public class MqttWaterLeaksWorker : BackgroundService
{
    private readonly ILogger<MqttWaterLeaksWorker> _logger;
    private readonly MqttSettings _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;
    private readonly WorkersSettings _workersSettings;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailNotificationService _emailNotificationService;

    public MqttWaterLeaksWorker(ILogger<MqttWaterLeaksWorker> logger, MqttSettings configuration, IServiceScopeFactory scopeFactory,
        MqttFactory mqttFactory, WorkersSettings workersSettings, IDateTimeService dateTimeService, EmailNotificationService emailNotificationService)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _mqttFactory = mqttFactory;
        _workersSettings = workersSettings;
        _dateTimeService = dateTimeService;
        _emailNotificationService = emailNotificationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = _mqttFactory.CreateManagedMqttClient();

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectingFailedAsync += LogConnectionFailure;
        client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLogging;

        await client.StartAsync(MqttHelpers.BuildOptions(_configuration));

        foreach (var topic in _workersSettings.MqttWaterLeaksTopics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        await client.SubscribeAsync(MqttHelpers.BuildTopicFilters(_mqttFactory, _workersSettings.MqttWaterLeaksTopics));

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
        var message = MqttHelpers.DeserializeWaterLeakMessage(e);

        if (_configuration.LogMessages)
        {
            var readableMessage = MqttHelpers.GetReadableWaterLeakMessage(message);
            _logger.LogInformation("{Output}", readableMessage);
        }

        using var scope = _scopeFactory.CreateScope();

        var locationName = e.ApplicationMessage.Topic.Split("/").LastOrDefault() ?? "Unknown";

        // TODO: handle alert latching with dictionary
        if (message.Water_Leak)
        {
            await NotifyLeak(locationName, message);
        }

        if (message.Battery_Low)
        {
            await NotifyBatteryLow(locationName, message);
        }

    }

    private Task NotifyLeak(string locationName, MqttWaterLeakMessage message)
    {
        var time = _dateTimeService.MomentWithOffset;

        _logger.LogWarning("Water leak detected: {LocationName}", locationName);

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject($"Water leak detected: {locationName}");

            e.AddLine($"Water leak detected at {locationName}.");
            e.AddLine();
            e.AddLine($"Time: {time}");
            e.AddLine($"Battery: {message.Battery}%");
        }, default);
    }

    private Task NotifyBatteryLow(string locationName, MqttWaterLeakMessage message)
    {
        var time = _dateTimeService.MomentWithOffset;

        _logger.LogWarning("Water leak sensor battery low: {LocationName}", locationName);

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject($"Water leak sensor battery low: {locationName}");

            e.AddLine($"Water leak sensor battery low at {locationName}.");
            e.AddLine();
            e.AddLine($"Time: {time}");
            e.AddLine($"Battery: {message.Battery}%");
        }, default);
    }

}
