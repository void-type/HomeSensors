using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;
using HomeSensors.Web.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Api;

[ApiRoute("temperatures")]
public class TemperatureApiController : ControllerBase
{
    private readonly TemperatureRepository _temperatureRepository;

    public TemperatureApiController(TemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    [HttpPost]
    [Route("time-series")]
    [ProducesResponseType(typeof(List<GraphTimeSeries>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetTimeSeries([FromBody] GraphRequest request)
    {
        var series = await _temperatureRepository.GetTimeSeries(request.StartTime, request.EndTime, request.IntervalMinutes);

        return HttpResponder.Respond(series);
    }

    [HttpPost]
    [Route("current")]
    [ProducesResponseType(typeof(List<GraphCurrentReading>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetCurrentReadings()
    {
        var readings = await _temperatureRepository.GetCurrentReadings();

        return HttpResponder.Respond(readings);
    }
}
