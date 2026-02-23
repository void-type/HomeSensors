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

public class MqttWaterLeaksWorker : BackgroundService
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

        logger.LogInformation("Enabling background job: {JobName}.",
            nameof(MqttWaterLeaksWorker));
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
            var timer = new PeriodicTimer(_betweenTicks);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                var startTime = Stopwatch.GetTimestamp();

                try
                {
                    _logger.LogInformation("{JobName} job is starting.", nameof(MqttWaterLeaksWorker));

                    await RefreshTopicSubscriptionsAsync();

                    using var scope = _scopeFactory.CreateScope();
                    var waterLeakAlertService = scope.ServiceProvider.GetRequiredService<WaterLeakAlertService>();
                    await waterLeakAlertService.CheckInactiveAsync();
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
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttWaterLeaksWorker));

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
