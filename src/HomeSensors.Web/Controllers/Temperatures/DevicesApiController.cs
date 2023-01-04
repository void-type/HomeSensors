using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
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
[ApiRoute("temperatures/devices")]
public class DevicesApiController : ControllerBase
{
    private readonly TemperatureCachedRepository _cachedTemperatureRepository;

    public DevicesApiController(TemperatureCachedRepository temperatureRepository)
    {
        _cachedTemperatureRepository = temperatureRepository;
    }

    [HttpPost]
    [Route("all")]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAll()
    {
        var readings = await _cachedTemperatureRepository.GetAllDevices();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("update")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Update([FromBody] UpdateDeviceRequest request)
    {
        var result = await _cachedTemperatureRepository.UpdateDevice(request);
        return HttpResponder.Respond(result);
    }
}
