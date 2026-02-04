using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Services.MqttDiscovery;

public class MqttDiscoveryService
{
    private readonly ILogger<MqttDiscoveryService> _logger;
    private readonly MqttSettings _configuration;
    private readonly MqttFactory _mqttFactory;
    private readonly IHubContext<TemperaturesHub> _tempHubContext;
    private readonly IDateTimeService _dateTimeService;
    private MqttDiscoveryClientState? _clientState;

    public MqttDiscoveryService(ILogger<MqttDiscoveryService> logger, MqttSettings configuration,
        MqttFactory mqttFactory, IHubContext<TemperaturesHub> tempHubContext, IDateTimeService dateTimeService)
    {
        _logger = logger;
        _configuration = configuration;
        _mqttFactory = mqttFactory;
        _tempHubContext = tempHubContext;
        _dateTimeService = dateTimeService;
    }

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

        var client = _mqttFactory.CreateManagedMqttClient();

        _clientState = new(request.Topics, client);

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectedAsync += OnStateChangeAsync;
        client.ConnectionStateChangedAsync += OnStateChangeAsync;
        client.ConnectingFailedAsync += OnConnectionFailureAsync;
        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await client.StartAsync(_configuration.GetClientOptions());

        foreach (var topic in request.Topics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        try
        {
            await client.SubscribeAsync(_mqttFactory.GetTopicFilters(request.Topics));
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

            if (_configuration.LogMessages)
            {
                MqttHelpers.LogMqttPayload(_logger, payload);
            }

            var message = new MqttDiscoveryMessage(_dateTimeService.MomentWithOffset, e.ApplicationMessage.Topic, payload);

            await _tempHubContext.Clients.All.SendAsync(TemperaturesHub.NewDiscoveryMessageMessageName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttDiscoveryService));
        }
    }

    private async Task OnStateChangeAsync(EventArgs e)
    {
        var status = GetClientStatus();
        _logger.LogInformation("MQTT client connection state changed to {State}.", status.IsConnected ? "Connected" : "Disconnected");
        await BroadcastStatusAsync(status);
    }

    private async Task OnConnectionFailureAsync(ConnectingFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "MQTT client failed to connect.");
        await BroadcastStatusAsync();
    }

    private async Task BroadcastStatusAsync(MqttDiscoveryClientStatus? status = null)
    {
        await _tempHubContext.Clients.All.SendAsync(TemperaturesHub.UpdateDiscoveryStatusMessageName, status ?? GetClientStatus());
    }
}
