using HomeSensors.Data.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Temperatures;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[ApiRoute("temperatures")]
public class TemperatureApiController : ControllerBase
{
    private readonly CachedTemperatureRepository _temperatureRepository;

    public TemperatureApiController(CachedTemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    [HttpPost]
    [Route("current-readings")]
    [ProducesResponseType(typeof(List<GraphCurrentReading>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadings()
    {
        var readings = await _temperatureRepository.GetCurrentReadings();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("inactive-devices")]
    [ProducesResponseType(typeof(List<InactiveDevice>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetInactiveDevices()
    {
        var readings = await _temperatureRepository.GetInactiveDevices();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("time-series")]
    [ProducesResponseType(typeof(List<GraphTimeSeries>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeries([FromBody] GraphTimeSeriesRequest request)
    {
        var series = await _temperatureRepository.GetTimeSeries(request);
        return HttpResponder.Respond(series);
    }
}
