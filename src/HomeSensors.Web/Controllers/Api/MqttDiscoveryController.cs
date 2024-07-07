using HomeSensors.Web.Services.MqttDiscovery;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/mqtt-discovery")]
public class MqttDiscoveryController : ControllerBase
{
    private readonly MqttDiscoveryService _discoveryService;

    public MqttDiscoveryController(MqttDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    [HttpGet]
    [Route("status")]
    [ProducesResponseType(typeof(MqttDiscoveryClientStatus), 200)]
    public IActionResult Status()
    {
        return HttpResponder.Respond(_discoveryService.GetClientStatus());
    }

    [HttpPost]
    [Route("setup")]
    [ProducesResponseType(typeof(MqttDiscoveryClientStatus), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Setup([FromBody] MqttDiscoverySetupRequest request)
    {
        return HttpResponder.Respond(await _discoveryService.SetupClient(request));
    }

    [HttpPost]
    [Route("teardown")]
    [ProducesResponseType(typeof(MqttDiscoveryClientStatus), 200)]
    public IActionResult Teardown()
    {
        return HttpResponder.Respond(_discoveryService.TeardownClient());
    }
}
