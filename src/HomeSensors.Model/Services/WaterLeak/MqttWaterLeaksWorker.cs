using HomeSensors.Model.Emailing;
using HomeSensors.Model.Json;
using HomeSensors.Model.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Diagnostics;
using System.Text.Json;
using VoidCore.Model.Guards;


namespace HomeSensors.Model.Services.WaterLeak;

public class MqttWaterLeaksWorker : BackgroundService
{
    private readonly ILogger<MqttWaterLeaksWorker> _logger;
    private readonly MqttSettings _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;
    private readonly MqttWaterLeaksSettings _workerSettings;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly TimeSpan _betweenTicks;

    public MqttWaterLeaksWorker(ILogger<MqttWaterLeaksWorker> logger, MqttSettings configuration, IServiceScopeFactory scopeFactory,
        MqttFactory mqttFactory, MqttWaterLeaksSettings workerSettings, EmailNotificationService emailNotificationService)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _mqttFactory = mqttFactory;
        _workerSettings = workerSettings;
        _emailNotificationService = emailNotificationService;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);

        logger.LogInformation("Enabling background job: {JobName}.",
            nameof(MqttWaterLeaksWorker));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = _mqttFactory.CreateManagedMqttClient();

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectingFailedAsync += LogConnectionFailure;
        client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLogging;

        await client.StartAsync(_configuration.GetClientOptions());

        var topics = _workerSettings.Devices.Select(x => x.Topic).ToArray();

        foreach (var topic in topics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        await client.SubscribeAsync(_mqttFactory.GetTopicFilters(topics));

        // The client is still running, we just loop here and the Delay will listen for the stop token.
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(MqttWaterLeaksWorker));

                using var scope = _scopeFactory.CreateScope();
                var waterLeakAlertService = scope.ServiceProvider.GetRequiredService<WaterLeakAlertService>();
                await waterLeakAlertService.CheckInactive();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttWaterLeaksWorker));
            }
            finally
            {
                _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(MqttWaterLeaksWorker), Stopwatch.GetElapsedTime(startTime));
            }
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
            await _emailNotificationService.NotifyError($"Topic: {e.ApplicationMessage.Topic}\nPayload string: {e.GetPayloadString()}", $"Exception thrown in {nameof(MqttWaterLeaksWorker)}", ex);
        }
    }

    private async Task ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = DeserializeWaterLeakMessage(e);

        var name = Array.Find(_workerSettings.Devices, x => x.Topic == e.ApplicationMessage.Topic)?.Name ??
            e.ApplicationMessage.Topic.Split("/").LastOrDefault() ??
            "Unknown";

        var message = new MqttWaterLeakDeviceMessage(name, payload);

        if (_configuration.LogMessages)
        {
            var readableMessage = GetReadableWaterLeakMessage(message);
            MqttHelpers.LogMqttPayload(_logger, readableMessage);
        }

        using var scope = _scopeFactory.CreateScope();
        var waterLeakAlertService = scope.ServiceProvider.GetRequiredService<WaterLeakAlertService>();

        await waterLeakAlertService.Process(message);
    }

    public static MqttWaterLeakDeviceMessagePayload DeserializeWaterLeakMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.GetPayloadString();

        return JsonSerializer.Deserialize<MqttWaterLeakDeviceMessagePayload>(payload, JsonHelpers.JsonOptions)
            .EnsureNotNull();
    }

    public static string GetReadableWaterLeakMessage(MqttWaterLeakDeviceMessage message)
    {
        return $"Location: {message.LocationName} | Water leak: {message.Payload.Water_Leak} | Battery low: {message.Payload.Battery_Low} | Battery: {message.Payload.Battery}%";
    }
}
