using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using VoidCore.Model.Functional;

namespace HomeSensors.Web.Services.MqttDiscovery;

public partial class MqttDiscoveryService(ILogger<MqttDiscoveryService> logger, MqttSettings configuration,
    MqttFactory mqttFactory, IHubContext<TemperaturesHub> tempHubContext, TimeProvider timeProvider)
{
    private MqttDiscoveryClientState? _clientState;

    public MqttDiscoveryClientStatus GetClientStatus()
    {
        return new MqttDiscoveryClientStatus(
            Topics: _clientState?.Topics,
            IsCreated: _clientState?.Client is not null,
            IsConnected: _clientState?.Client?.IsConnected ?? false);
    }

    public async Task<VoidCore.Model.Functional.IResult> SetupClientAsync(MqttDiscoverySetupRequest request)
    {
        if (_clientState is not null)
        {
            return Result.Fail(new Failure("Client already exists. End existing before setting up a new one."));
        }

        var client = mqttFactory.CreateManagedMqttClient();

        _clientState = new(request.Topics, client);

        ConnectingClient(logger);

        client.ConnectedAsync += OnStateChangeAsync;
        client.ConnectionStateChangedAsync += OnStateChangeAsync;
        client.ConnectingFailedAsync += OnConnectionFailureAsync;
        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await client.StartAsync(configuration.GetClientOptions());

        foreach (var topic in request.Topics)
        {
            SubscribingClientToTopic(logger, topic);
        }

        try
        {
            await client.SubscribeAsync(mqttFactory.GetTopicFilters(request.Topics));
        }
        catch (MqttProtocolViolationException ex)
        {
            await TeardownClientAsync();
            return Result.Fail(new Failure(ex.Message, "topics"));
        }

        return Result.Ok();
    }

    public async Task TeardownClientAsync()
    {
        if (_clientState?.Client is not null)
        {
            _clientState.Client.ConnectedAsync -= OnStateChangeAsync;
            _clientState.Client.ConnectionStateChangedAsync -= OnStateChangeAsync;
            _clientState.Client.ConnectingFailedAsync -= OnConnectionFailureAsync;
            _clientState.Client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;
            _clientState.Client.Dispose();
        }

        _clientState = null;

        await BroadcastStatusAsync();
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = e.GetPayloadString();

            if (configuration.LogMessages)
            {
                MqttHelpers.LogMqttPayload(logger, payload);
            }

            var message = new MqttDiscoveryMessage(timeProvider.GetUtcNow(), e.ApplicationMessage.Topic, payload);

            await tempHubContext.Clients.All.SendAsync(TemperaturesHub.NewDiscoveryMessageMessageName, message);
        }
        catch (Exception ex)
        {
            WorkerFailed(logger, ex, nameof(MqttDiscoveryService));
        }
    }

    private async Task OnStateChangeAsync(EventArgs e)
    {
        var status = GetClientStatus();
        ConnectionStateChanged(logger, status.IsConnected ? "Connected" : "Disconnected");
        await BroadcastStatusAsync(status);
    }

    private async Task OnConnectionFailureAsync(ConnectingFailedEventArgs e)
    {
        ClientFailedToConnect(logger, e.Exception);
        await BroadcastStatusAsync();
    }

    private async Task BroadcastStatusAsync(MqttDiscoveryClientStatus? status = null)
    {
        await tempHubContext.Clients.All.SendAsync(TemperaturesHub.UpdateDiscoveryStatusMessageName, status ?? GetClientStatus());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting Managed MQTT client.")]
    private static partial void ConnectingClient(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribing MQTT client to topic {Topic}.")]
    private static partial void SubscribingClientToTopic(ILogger logger, string topic);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown in {WorkerName}.")]
    private static partial void WorkerFailed(ILogger logger, Exception exception, string workerName);

    [LoggerMessage(Level = LogLevel.Information, Message = "MQTT client connection state changed to {State}.")]
    private static partial void ConnectionStateChanged(ILogger logger, string state);

    [LoggerMessage(Level = LogLevel.Error, Message = "MQTT client failed to connect.")]
    private static partial void ClientFailedToConnect(ILogger logger, Exception exception);
}
