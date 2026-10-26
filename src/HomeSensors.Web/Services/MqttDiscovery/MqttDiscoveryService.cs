using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using VoidCore.Model.Functional;

namespace HomeSensors.Web.Services.MqttDiscovery;

public class MqttDiscoveryService(ILogger<MqttDiscoveryService> logger, MqttSettings configuration,
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

        logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectedAsync += OnStateChangeAsync;
        client.ConnectionStateChangedAsync += OnStateChangeAsync;
        client.ConnectingFailedAsync += OnConnectionFailureAsync;
        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await client.StartAsync(configuration.GetClientOptions());

        foreach (var topic in request.Topics)
        {
            logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
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
            logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttDiscoveryService));
        }
    }

    private async Task OnStateChangeAsync(EventArgs e)
    {
        var status = GetClientStatus();
        logger.LogInformation("MQTT client connection state changed to {State}.", status.IsConnected ? "Connected" : "Disconnected");
        await BroadcastStatusAsync(status);
    }

    private async Task OnConnectionFailureAsync(ConnectingFailedEventArgs e)
    {
        logger.LogError(e.Exception, "MQTT client failed to connect.");
        await BroadcastStatusAsync();
    }

    private async Task BroadcastStatusAsync(MqttDiscoveryClientStatus? status = null)
    {
        await tempHubContext.Clients.All.SendAsync(TemperaturesHub.UpdateDiscoveryStatusMessageName, status ?? GetClientStatus());
    }
}
