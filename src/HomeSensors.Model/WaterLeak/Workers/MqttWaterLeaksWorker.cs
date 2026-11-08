using HomeSensors.Model.Data;
using HomeSensors.Model.Infrastructure.Emailing;
using HomeSensors.Model.Infrastructure.Json;
using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Model.WaterLeak.Models;
using HomeSensors.Model.WaterLeak.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Diagnostics;
using System.Text.Json;
using VoidCore.Model.Guards;

namespace HomeSensors.Model.WaterLeak.Workers;

public partial class MqttWaterLeaksWorker : BackgroundService
{
    private readonly ILogger<MqttWaterLeaksWorker> _logger;
    private readonly MqttSettings _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttFactory _mqttFactory;
    private readonly TimeSpan _betweenTicks;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IManagedMqttClient? _mqttClient;
    private List<string> _currentTopics = [];

    public MqttWaterLeaksWorker(ILogger<MqttWaterLeaksWorker> logger, MqttSettings configuration, IServiceScopeFactory scopeFactory,
        MqttFactory mqttFactory, MqttWaterLeaksSettings workerSettings)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _mqttFactory = mqttFactory;
        _betweenTicks = TimeSpan.FromMinutes(workerSettings.BetweenTicksMinutes);

        EnablingBackgroundJob(logger, nameof(MqttWaterLeaksWorker));
    }

    public async Task RefreshTopicSubscriptionsAsync()
    {
        await _refreshLock.WaitAsync();

        try
        {
            var newTopics = await GetTopicsAsync();

            var topicsToUnsubscribe = _currentTopics.Except(newTopics, StringComparer.Ordinal).ToList();
            await UnsubscribeFromTopicsAsync(topicsToUnsubscribe);

            var topicsToSubscribe = newTopics.Except(_currentTopics, StringComparer.Ordinal).ToList();
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

            ConnectingClient(_logger);

            _mqttClient.ConnectingFailedAsync += LogConnectionFailureAsync;
            _mqttClient.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLoggingAsync;

            await _mqttClient.StartAsync(_configuration.GetClientOptions());

            await RefreshTopicSubscriptionsAsync();

            // The client is still running, we just loop here and the Delay will listen for the stop token.
            var timer = new PeriodicTimer(_betweenTicks);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                var startTime = Stopwatch.GetTimestamp();

                try
                {
                    JobStarting(_logger, nameof(MqttWaterLeaksWorker));

                    await CheckInactiveDevicesAsync();
                }
                catch (Exception ex)
                {
                    WorkerFailed(_logger, ex, nameof(MqttWaterLeaksWorker));
                }
                finally
                {
                    var elapsed = Stopwatch.GetElapsedTime(startTime);
                    JobFinished(_logger, nameof(MqttWaterLeaksWorker), elapsed);
                }
            }
        }
        finally
        {
            _mqttClient?.Dispose();
            _mqttClient = null;
        }
    }

    private async Task CheckInactiveDevicesAsync()
    {
        await RefreshTopicSubscriptionsAsync();

        using var scope = _scopeFactory.CreateScope();
        var waterLeakAlertService = scope.ServiceProvider.GetRequiredService<WaterLeakAlertService>();
        await waterLeakAlertService.CheckInactiveAsync();
    }

    private async Task LogConnectionFailureAsync(ConnectingFailedEventArgs e)
    {
        ClientFailedToConnect(_logger, e.Exception);
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
            WorkerFailed(_logger, ex, nameof(MqttWaterLeaksWorker));

            using var scope = _scopeFactory.CreateScope();
            var emailNotificationService = scope.ServiceProvider.GetRequiredService<EmailNotificationService>();
            await emailNotificationService.NotifyErrorAsync($"Topic: {e.ApplicationMessage.Topic}\nPayload string: {e.GetPayloadString()}", $"Exception thrown in {nameof(MqttWaterLeaksWorker)}", ex);
        }
    }

    private async Task ProcessMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = DeserializeWaterLeakMessage(e);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var device = await dbContext.WaterLeakDevices
            .TagWith($"Query called from {nameof(MqttWaterLeaksWorker)}.")
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MqttTopic == e.ApplicationMessage.Topic);

        var name = device?.Name
            ?? e.ApplicationMessage.Topic
            ?? "Unknown";

        var inactiveLimitMinutes = device?.InactiveLimitMinutes ?? 90;

        var message = new MqttWaterLeakDeviceMessage(name, payload, inactiveLimitMinutes);

        if (_configuration.LogMessages)
        {
            var readableMessage = GetReadableWaterLeakMessage(message);
            MqttHelpers.LogMqttPayload(_logger, readableMessage);
        }

        var waterLeakAlertService = scope.ServiceProvider.GetRequiredService<WaterLeakAlertService>();

        await waterLeakAlertService.ProcessAsync(message);
    }

    private async Task<List<string>> GetTopicsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        return await dbContext.WaterLeakDevices
            .TagWith($"Query called from {nameof(MqttWaterLeaksWorker)}.")
            .Where(x => !string.IsNullOrWhiteSpace(x.MqttTopic))
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
            UnsubscribingFromTopic(_logger, topic);
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
            SubscribingToTopic(_logger, topicFilter.Topic);
        }

        await _mqttClient.SubscribeAsync(topicFilters);
    }

    public static MqttWaterLeakDeviceMessagePayload DeserializeWaterLeakMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.GetPayloadString();

        return JsonSerializer.Deserialize<MqttWaterLeakDeviceMessagePayload>(payload, JsonHelpers.JsonOptions)
            .EnsureNotNull();
    }

    public static string GetReadableWaterLeakMessage(MqttWaterLeakDeviceMessage message)
    {
        return $"Location: {message.LocationName} | Water leak: {message.Payload.WaterLeak} | Battery low: {message.Payload.BatteryLow} | Battery: {message.Payload.Battery}%";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Enabling background job: {JobName}.")]
    private static partial void EnablingBackgroundJob(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting Managed MQTT client.")]
    private static partial void ConnectingClient(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job is starting.")]
    private static partial void JobStarting(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown in {WorkerName}.")]
    private static partial void WorkerFailed(ILogger logger, Exception exception, string workerName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{JobName} job is finished in {ElapsedTime}.")]
    private static partial void JobFinished(ILogger logger, string jobName, TimeSpan elapsedTime);

    [LoggerMessage(Level = LogLevel.Error, Message = "MQTT client failed to connect.")]
    private static partial void ClientFailedToConnect(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unsubscribing from MQTT topic {Topic}.")]
    private static partial void UnsubscribingFromTopic(ILogger logger, string topic);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribing to MQTT topic {Topic}.")]
    private static partial void SubscribingToTopic(ILogger logger, string topic);
}
