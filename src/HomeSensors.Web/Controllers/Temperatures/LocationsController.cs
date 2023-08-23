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
[ApiRoute("temperatures/locations")]
public class LocationsController : ControllerBase
{
    private readonly TemperatureLocationRepository _locationRepository;

    public LocationsController(TemperatureLocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    [HttpPost]
    [Route("all")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(List<Location>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAll()
    {
        var series = await _locationRepository.GetAll();
        return HttpResponder.Respond(series);
    }

    [HttpPost]
    [Route("check-limits")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(List<CheckLimitResult>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> CheckLimits(DateTimeOffset lastCheck)
    {
        var limitResults = await _locationRepository.CheckLimits(lastCheck);
        return HttpResponder.Respond(limitResults);
    }

    [HttpPost]
    [Route("create")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request)
    {
        var result = await _locationRepository.Create(request);
        return HttpResponder.Respond(result);
    }

    [HttpPost]
    [Route("update")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Update([FromBody] UpdateLocationRequest request)
    {
        var result = await _locationRepository.Update(request);
        return HttpResponder.Respond(result);
    }
}
