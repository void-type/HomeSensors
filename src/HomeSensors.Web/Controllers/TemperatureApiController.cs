using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[ApiRoute("temperatures")]
public class TemperatureApiController : ControllerBase
{
    private readonly TemperatureCachedRepository _cachedTemperatureRepository;
    private readonly TemperatureDeviceRepository _deviceRepository;

    public TemperatureApiController(TemperatureCachedRepository temperatureRepository, TemperatureDeviceRepository temperatureDeviceRepository)
    {
        _cachedTemperatureRepository = temperatureRepository;
        _deviceRepository = temperatureDeviceRepository;
    }

    [HttpPost]
    [Route("current-readings")]
    [ProducesResponseType(typeof(List<CurrentReading>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadings()
    {
        var readings = await _cachedTemperatureRepository.GetCurrentReadings();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("time-series")]
    [ProducesResponseType(typeof(List<GraphTimeSeries>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeries([FromBody] GraphTimeSeriesRequest request)
    {
        var series = await _cachedTemperatureRepository.GetTimeSeriesReadings(request);
        return HttpResponder.Respond(series);
    }

    [HttpPost]
    [Route("locations")]
    [ProducesResponseType(typeof(List<Location>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetLocations()
    {
        var series = await _cachedTemperatureRepository.GetAllLocations();
        return HttpResponder.Respond(series);
    }

    [HttpPost]
    [Route("inactive-devices")]
    [ProducesResponseType(typeof(List<InactiveDevice>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetInactiveDevices()
    {
        var readings = await _deviceRepository.GetInactive();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("lost-devices")]
    [ProducesResponseType(typeof(List<LostDevice>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetLostDevices()
    {
        var readings = await _deviceRepository.GetLost();
        return HttpResponder.Respond(readings);
    }
}
