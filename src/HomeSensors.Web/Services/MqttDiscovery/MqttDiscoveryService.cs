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
        _tempHubContext = tempHubContext;
    }

    public MqttDiscoveryClientStatus GetClientStatus()
    {
        return new MqttDiscoveryClientStatus(
            Topics: _clientState?.Topics,
            IsCreated: _clientState?.Client is not null,
            IsConnected: _clientState?.Client?.IsConnected ?? false);
    }

    public async Task<IResult<MqttDiscoveryClientStatus>> SetupClientAsync(MqttDiscoverySetupRequest request)
    {
        if (_clientState is not null)
        {
            return Result.Fail<MqttDiscoveryClientStatus>(new Failure("Client already exists. End existing before setting up a new one."));
        }

        var client = _mqttFactory.CreateManagedMqttClient();

        _clientState = new(request.Topics, client);

        _logger.LogInformation("Connecting Managed MQTT client.");

        client.ConnectingFailedAsync += LogConnectionFailureAsync;
        client.ApplicationMessageReceivedAsync += ProcessMessageWithExceptionLoggingAsync;

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
            TeardownClient();
            return Result.Fail<MqttDiscoveryClientStatus>(new Failure(ex.Message, "topics"));
        }

        return Result.Ok(GetClientStatus());
    }

    public MqttDiscoveryClientStatus TeardownClient()
    {
        if (_clientState is not null)
        {
            _clientState.Client.Dispose();
            _clientState = null;
        }

        return GetClientStatus();
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
            _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(MqttDiscoveryService));
        }
    }

    private async Task ProcessMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.GetPayloadString();

        if (_configuration.LogMessages)
        {
            MqttHelpers.LogMqttPayload(_logger, payload);
        }

        var message = new MqttDiscoveryMessage(_dateTimeService.MomentWithOffset, e.ApplicationMessage.Topic, payload);

        await _tempHubContext.Clients.All.SendAsync(TemperaturesHub.NewDiscoveryMessageMessageName, message);
    }
}
