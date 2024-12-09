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
        return _discoveryService.GetClientStatus()
            .Map(HttpResponder.Respond);
    }

    [HttpPost]
    [Route("setup")]
    [ProducesResponseType(typeof(MqttDiscoveryClientStatus), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SetupAsync([FromBody] MqttDiscoverySetupRequest request)
    {
        return await _discoveryService.SetupClientAsync(request)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [Route("teardown")]
    [ProducesResponseType(typeof(MqttDiscoveryClientStatus), 200)]
    public IActionResult Teardown()
    {
        return _discoveryService.TeardownClient()
            .Map(HttpResponder.Respond);
    }
}
