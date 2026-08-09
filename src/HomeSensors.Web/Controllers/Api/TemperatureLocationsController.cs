using HomeSensors.Model.Temperature.Models;
using HomeSensors.Model.Temperature.Repositories;
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
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _locationRepository.GetAllAsync(cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpGet]
    [Route("check-limits")]
    [ProducesResponseType(typeof(List<TemperatureCheckLimitResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> CheckLimitsAsync(DateTimeOffset since, bool isAveragingEnabled, CancellationToken cancellationToken)
    {
        return await _locationRepository.CheckLimitsAsync(since, isAveragingEnabled, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] TemperatureLocationSaveRequest request, CancellationToken cancellationToken)
    {
        return await _locationRepository.SaveAsync(request, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        return await _locationRepository.DeleteAsync(id, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }
}
