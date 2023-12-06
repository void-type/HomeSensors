using HomeSensors.Model.Mqtt;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using VoidCore.Model.Functional;

namespace HomeSensors.Web.Services;

public class MqttFeedDiscoveryService
{
    private readonly ILogger<MqttFeedDiscoveryService> _logger;
    private readonly MqttSettings _configuration;
    private readonly MqttFactory _mqttFactory;
    private readonly IHubContext<TemperaturesHub> _tempHubContext;
    private IManagedMqttClient? _client;

    public bool ClientExists => _client is not null;

    public MqttFeedDiscoveryService(ILogger<MqttFeedDiscoveryService> logger, MqttSettings configuration,
        MqttFactory mqttFactory, IHubContext<TemperaturesHub> tempHubContext)
    {
        _logger = logger;
        _configuration = configuration;
        _mqttFactory = mqttFactory;
        _tempHubContext = tempHubContext;
        _tempHubContext = tempHubContext;
    }

    public ClientStatus GetClientStatus()
    {
        return new ClientStatus(
            IsCreated: _client is not null,
            IsConnected: _client?.IsConnected ?? false);
    }

    public async Task<IResult<ClientStatus>> SetupClient(SetupRequest request)
    {
        if (_client is not null)
        {
            return Result.Fail<ClientStatus>(new Failure("Client already exists. Tear down before setting up a new one."));
        }

        _client = _mqttFactory.CreateManagedMqttClient();

        _logger.LogInformation("Connecting Managed MQTT client.");

        _client.ConnectingFailedAsync += LogConnectionFailure;
        _client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLogging;

        await _client.StartAsync(MqttHelpers.BuildOptions(_configuration));

        foreach (var topic in request.Topics)
        {
            _logger.LogInformation("Subscribing MQTT client to topic {Topic}.", topic);
        }

        await _client.SubscribeAsync(MqttHelpers.BuildTopicFilters(_mqttFactory, request.Topics));

        return Result.Ok(GetClientStatus());
    }

    public ClientStatus TeardownClient()
    {
        if (_client is not null)
        {
            _client.Dispose();
            _client = null;
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
        var message = MqttHelpers.DeserializeMessage(e);

        var readableMessage = MqttHelpers.GetReadableMessage(message);

        if (_configuration.LogMessages)
        {
            _logger.LogInformation("{Output}", readableMessage);
        }

        await _tempHubContext.Clients.All.SendAsync(TemperaturesHub.newMessageMessageName, readableMessage);
    }

    public record ClientStatus(bool IsCreated, bool IsConnected);
    public record SetupRequest(string[] Topics);
}
