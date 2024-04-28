using HomeSensors.Model.Helpers;
using HomeSensors.Model.Mqtt;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Services;

public partial class MqttFeedDiscoveryService
{
    private readonly ILogger<MqttFeedDiscoveryService> _logger;
    private readonly MqttSettings _configuration;
    private readonly MqttFactory _mqttFactory;
    private readonly IHubContext<TemperaturesHub> _tempHubContext;
    private readonly IDateTimeService _dateTimeService;
    private ClientState? _clientState;

    public MqttFeedDiscoveryService(ILogger<MqttFeedDiscoveryService> logger, MqttSettings configuration,
        MqttFactory mqttFactory, IHubContext<TemperaturesHub> tempHubContext, IDateTimeService dateTimeService)
    {
        _logger = logger;
        _configuration = configuration;
        _mqttFactory = mqttFactory;
        _tempHubContext = tempHubContext;
        _dateTimeService = dateTimeService;
        _tempHubContext = tempHubContext;
    }

    public ClientStatus GetClientStatus()
    {
        return new ClientStatus(
            Topics: _clientState?.Topics,
            IsCreated: _clientState?.Client is not null,
            IsConnected: _clientState?.Client?.IsConnected ?? false);
    }

    public async Task<IResult<ClientStatus>> SetupClient(SetupRequest request)
    {
        if (_clientState is not null)
        {
            return Result.Fail<ClientStatus>(new Failure("Client already exists. End existing before setting up a new one."));
        }

        var client = _mqttFactory.CreateManagedMqttClient();

        _clientState = new(request.Topics, client);

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectingFailedAsync += LogConnectionFailure;
        client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLogging;

        await client.StartAsync(MqttHelpers.BuildOptions(_configuration));

        foreach (var topic in request.Topics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        await client.SubscribeAsync(MqttHelpers.BuildTopicFilters(_mqttFactory, request.Topics));

        return Result.Ok(GetClientStatus());
    }

    public ClientStatus TeardownClient()
    {
        if (_clientState is not null)
        {
            _clientState.Client.Dispose();
            _clientState = null;
        }

        return GetClientStatus();
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
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttFeedDiscoveryService));
        }
    }

    private async Task ProcessMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = MqttHelpers.GetPayloadString(e);

        if (_configuration.LogMessages)
        {
            LoggerMessages.LogMqttPayload(_logger, payload);
        }

        var message = new DiscoveryMessage(_dateTimeService.MomentWithOffset, e.ApplicationMessage.Topic, payload);

        await _tempHubContext.Clients.All.SendAsync(TemperaturesHub.newMessageMessageName, message);
    }

    public record ClientState(string[] Topics, IManagedMqttClient Client);
    public record ClientStatus(string[]? Topics, bool IsCreated, bool IsConnected);
    public record SetupRequest(string[] Topics);
    public record DiscoveryMessage(DateTimeOffset Time, string Topic, string Payload);
}
