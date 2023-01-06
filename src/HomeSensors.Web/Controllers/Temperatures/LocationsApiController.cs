using HomeSensors.Model.Repositories;
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
[ApiRoute("temperatures/locations")]
public class LocationsApiController : ControllerBase
{
    private readonly TemperatureCachedRepository _cachedTemperatureRepository;
    private readonly TemperatureLocationRepository _locationRepository;

    public LocationsApiController(TemperatureCachedRepository temperatureRepository, TemperatureLocationRepository locationRepository)
    {
        _cachedTemperatureRepository = temperatureRepository;
        _locationRepository = locationRepository;
    }

    [HttpPost]
    [Route("all")]
    [ProducesResponseType(typeof(List<Location>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAll()
    {
        var series = await _cachedTemperatureRepository.GetAllLocations();
        return HttpResponder.Respond(series);
    }

    [HttpPost]
    [Route("check-limits")]
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
        var result = await _cachedTemperatureRepository.CreateLocation(request);
        return HttpResponder.Respond(result);
    }

    [HttpPost]
    [Route("update")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Update([FromBody] UpdateLocationRequest request)
    {
        var result = await _cachedTemperatureRepository.UpdateLocation(request);
        return HttpResponder.Respond(result);
    }
}
