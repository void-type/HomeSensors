using HomeSensors.Web.Services;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using static HomeSensors.Web.Services.MqttFeedDiscoveryService;

namespace HomeSensors.Web.Controllers.Temperatures;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[ApiRoute("temperatures/mqtt-feed-discovery")]
public class MqttFeedDiscoveryController : ControllerBase
{
    private readonly MqttFeedDiscoveryService _discoveryService;

    public MqttFeedDiscoveryController(MqttFeedDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    [HttpGet]
    [Route("status")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ClientStatus), 200)]
    public IActionResult Status()
    {
        return HttpResponder.Respond(_discoveryService.GetClientStatus());
    }

    [HttpPost]
    [Route("setup")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ClientStatus), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request)
    {
        return HttpResponder.Respond(await _discoveryService.SetupClient(request));
    }

    [HttpPost]
    [Route("teardown")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ClientStatus), 200)]
    public IActionResult Teardown()
    {
        return HttpResponder.Respond(_discoveryService.TeardownClient());
    }
}
