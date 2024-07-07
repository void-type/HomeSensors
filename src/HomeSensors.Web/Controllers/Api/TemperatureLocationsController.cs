using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperatures-locations")]
public class TemperatureLocationsController : ControllerBase
{
    private readonly TemperatureLocationRepository _locationRepository;

    public TemperatureLocationsController(TemperatureLocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<TemperatureLocationResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAll()
    {
        var series = await _locationRepository.GetAll();
        return HttpResponder.Respond(series);
    }

    [HttpGet]
    [Route("check-limits")]
    [ProducesResponseType(typeof(List<TemperatureCheckLimitResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> CheckLimits(DateTimeOffset lastCheck)
    {
        var limitResults = await _locationRepository.CheckLimits(lastCheck);
        return HttpResponder.Respond(limitResults);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Save([FromBody] TemperatureLocationSaveRequest request)
    {
        var result = await _locationRepository.Save(request);
        return HttpResponder.Respond(result);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _locationRepository.Delete(id);

        return HttpResponder.Respond(result);
    }
}
