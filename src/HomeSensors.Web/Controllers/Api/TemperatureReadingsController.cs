using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Api;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[Route(ApiRouteAttribute.BasePath + "/temperatures-readings")]
public class TemperatureReadingsController : ControllerBase
{
    private readonly TemperatureCachedRepository _cachedTemperatureRepository;

    public TemperatureReadingsController(TemperatureCachedRepository cachedRepository)
    {
        _cachedTemperatureRepository = cachedRepository;
    }

    [HttpGet]
    [Route("current")]
    [ProducesResponseType(typeof(List<TemperatureReadingResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadings()
    {
        var readings = await _cachedTemperatureRepository.GetCurrentReadings();
        return HttpResponder.Respond(readings);
    }

    [HttpGet]
    [Route("time-series")]
    [ProducesResponseType(typeof(List<TemperatureTimeSeriesResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeries([FromBody] TemperatureTimeSeriesRequest request)
    {
        var series = await _cachedTemperatureRepository.GetTimeSeriesReadings(request);
        return HttpResponder.Respond(series);
    }
}
