using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperatures-readings")]
public class TemperatureReadingsController : ControllerBase
{
    private readonly TemperatureReadingRepository _readingRepository;

    public TemperatureReadingsController(TemperatureReadingRepository readingRepository)
    {
        _readingRepository = readingRepository;
    }

    [HttpGet]
    [Route("current")]
    [ProducesResponseType(typeof(List<TemperatureReadingResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadings()
    {
        var readings = await _readingRepository.GetCurrentCached();
        return HttpResponder.Respond(readings);
    }

    [HttpGet]
    [Route("location/{locationId}")]
    [ProducesResponseType(typeof(List<TemperatureReadingResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadingForLocation(long locationId)
    {
        var reading = await _readingRepository.GetCurrentForLocationCached(locationId);

        if (reading.HasNoValue)
        {
            return NotFound();
        }

        return HttpResponder.Respond(reading);
    }

    [HttpPost]
    [Route("time-series")]
    [ProducesResponseType(typeof(List<TemperatureTimeSeriesResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeries([FromBody] TemperatureTimeSeriesRequest request)
    {
        var series = await _readingRepository.GetTimeSeriesCached(request);
        return HttpResponder.Respond(series);
    }
}
