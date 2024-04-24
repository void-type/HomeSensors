using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Temperatures;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[Route("api/temperatures/readings")]
public class ReadingsController : ControllerBase
{
    private readonly TemperatureCachedRepository _cachedTemperatureRepository;

    public ReadingsController(TemperatureCachedRepository cachedRepository)
    {
        _cachedTemperatureRepository = cachedRepository;
    }

    [HttpPost]
    [Route("current")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(List<Reading>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadings()
    {
        var readings = await _cachedTemperatureRepository.GetCurrentReadings();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [Route("time-series")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(List<TimeSeries>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeries([FromBody] TimeSeriesRequest request)
    {
        var series = await _cachedTemperatureRepository.GetTimeSeriesReadings(request);
        return HttpResponder.Respond(series);
    }
}
