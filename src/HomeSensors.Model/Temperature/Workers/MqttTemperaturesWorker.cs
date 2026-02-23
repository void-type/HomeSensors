using HomeSensors.Model.Data;
using HomeSensors.Model.Infrastructure.Json;
using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Model.Temperature.Entities;
using HomeSensors.Model.Temperature.Helpers;
using HomeSensors.Model.Temperature.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text.Json;
using VoidCore.Model.Guards;

namespace HomeSensors.Model.Temperature.Workers;

public class MqttTemperaturesWorker : BackgroundService
{
    private readonly ILogger<MqttTemperaturesWorker> _logger;
    private readonly MqttSettings _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IManagedMqttClient? _mqttClient;
    private List<string> _currentTopics = [];

    public MqttTemperaturesWorker(ILogger<MqttTemperaturesWorker> logger, MqttSettings configuration, IServiceScopeFactory scopeFactory,
        MqttFactory mqttFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _mqttFactory = mqttFactory;

        logger.LogInformation("Enabling background job: {JobName}.",
            nameof(MqttTemperaturesWorker));
    }

    public async Task RefreshTopicSubscriptionsAsync()
    {
        await _refreshLock.WaitAsync();

        try
        {
            var newTopics = await GetTopicsAsync();

            var topicsToUnsubscribe = _currentTopics.Except(newTopics).ToList();
            await UnsubscribeFromTopicsAsync(topicsToUnsubscribe);

            var topicsToSubscribe = newTopics.Except(_currentTopics).ToList();
            await SubscribeToTopicsAsync(topicsToSubscribe);

            _currentTopics = newTopics;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _mqttClient = _mqttFactory.CreateManagedMqttClient();

            _logger.LogInformation("Connecting Managed MQTT client.");

            _mqttClient.ConnectingFailedAsync += LogConnectionFailureAsync;
            _mqttClient.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLoggingAsync;

            await _mqttClient.StartAsync(_configuration.GetClientOptions());

            await RefreshTopicSubscriptionsAsync();

            // The client is still running, we just loop here and the Delay will listen for the stop token.
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(300000, stoppingToken);

                // Every 5 minutes, refresh topics (in case the job is running as a service and won't be notified of device changes).
                await RefreshTopicSubscriptionsAsync();
            }
        }
        finally
        {
            _mqttClient?.Dispose();
            _mqttClient = null;
        }
    }

    private async Task LogConnectionFailureAsync(ConnectingFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "MQTT client failed to connect.");
        await Task.CompletedTask;
    }

    private async Task ProcessMessageWithExceptionLoggingAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            await ProcessMessageAsync(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttTemperaturesWorker));
        }
    }

    private async Task ProcessMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = DeserializeTemperatureMessage(e);

        if (_configuration.LogMessages)
        {
            var readableMessage = GetReadableTemperatureMessage(message);
            MqttHelpers.LogMqttPayload(_logger, readableMessage);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var device = await dbContext.TemperatureDevices
            .TagWith($"Query called from {nameof(MqttTemperaturesWorker)}.")
            .FirstOrDefaultAsync(x => x.MqttTopic == e.ApplicationMessage.Topic);

        if (device?.IsRetired != false || !device.TemperatureLocationId.HasValue)
        {
            return;
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
                TemperatureLocationId = device.TemperatureLocationId.Value
            });

        await dbContext.SaveChangesAsync();
    }

    private async Task<List<string>> GetTopicsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        return await dbContext.TemperatureDevices
            .TagWith($"Query called from {nameof(MqttTemperaturesWorker)}.")
            .Where(x => !x.IsRetired && !string.IsNullOrWhiteSpace(x.MqttTopic))
            .Select(x => x.MqttTopic)
            .ToListAsync();
    }

    private async Task UnsubscribeFromTopicsAsync(List<string> topics)
    {
        if (_mqttClient is null)
        {
            return;
        }

        if (topics.Count < 1)
        {
            return;
        }

        foreach (var topic in topics)
        {
            _logger.LogInformation("Unsubscribing from MQTT topic {Topic}.", topic);
        }

        await _mqttClient.UnsubscribeAsync(topics);
    }

    private async Task SubscribeToTopicsAsync(List<string> topics)
    {
        if (_mqttClient is null)
        {
            return;
        }

        if (topics.Count < 1)
        {
            return;
        }

        var topicFilters = _mqttFactory.GetTopicFilters(topics);

        foreach (var topicFilter in topicFilters)
        {
            _logger.LogInformation("Subscribing to MQTT topic {Topic}.", topicFilter.Topic);
        }

        await _mqttClient.SubscribeAsync(topicFilters);
    }

    public static MqttTemperatureDeviceMessage DeserializeTemperatureMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.GetPayloadString();

        return JsonSerializer.Deserialize<MqttTemperatureDeviceMessage>(payload, JsonHelpers.JsonOptions)
            .EnsureNotNull();
    }

    public static string GetReadableTemperatureMessage(MqttTemperatureDeviceMessage message)
    {
        return $"{message.Time.ToLocalTime()} | {message.Model}/{message.Id}/{message.Channel} | {TemperatureHelpers.FormatTemp(message.Temperature_C ?? -1000)} | {message.Humidity}%";
    }
}
