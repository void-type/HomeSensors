using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperature-locations")]
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
    public async Task<IActionResult> GetAllAsync()
    {
        return await _locationRepository.GetAllAsync()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpGet]
    [Route("check-limits")]
    [ProducesResponseType(typeof(List<TemperatureCheckLimitResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> CheckLimitsAsync(DateTimeOffset since, bool isAveragingEnabled)
    {
        return await _locationRepository.CheckLimitsAsync(since, isAveragingEnabled)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] TemperatureLocationSaveRequest request)
    {
        return await _locationRepository.SaveAsync(request)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        return await _locationRepository.DeleteAsync(id)
            .MapAsync(HttpResponder.Respond);
    }
}
