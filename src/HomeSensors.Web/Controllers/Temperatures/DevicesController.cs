using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Temperatures;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[Route(ApiRouteAttribute.BasePath + "/temperatures/devices")]
public class DevicesController : ControllerBase
{
    private readonly TemperatureDeviceRepository _deviceRepository;

    public DevicesController(TemperatureDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    [HttpPost]
    [Route("all")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAll()
    {
        var readings = await _deviceRepository.GetAll();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("update")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Update([FromBody] DeviceUpdateRequest request)
    {
        var result = await _deviceRepository.Update(request);
        return HttpResponder.Respond(result);
    }
}
