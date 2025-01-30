using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperature-readings")]
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
    public async Task<IActionResult> GetCurrentReadingsAsync()
    {
        return await _readingRepository.GetCurrentCachedAsync()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpGet]
    [Route("location/{locationId}")]
    [ProducesResponseType(typeof(List<TemperatureReadingResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadingForLocationAsync(long locationId)
    {
        return await _readingRepository.GetCurrentForLocationCachedAsync(locationId)
            .ToResultAsync(new Failure("No current reading for location.", nameof(locationId)))
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [Route("time-series")]
    [ProducesResponseType(typeof(List<TemperatureTimeSeriesResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeriesAsync([FromBody] TemperatureTimeSeriesRequest request)
    {
        return await _readingRepository.GetTimeSeriesCachedAsync(request)
            .MapAsync(HttpResponder.Respond);
    }
}
