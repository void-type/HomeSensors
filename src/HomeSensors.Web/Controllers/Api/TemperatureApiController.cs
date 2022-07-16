using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;
using HomeSensors.Web.Models;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Api;

[ApiRoute("temps")]
public class TemperatureApiController : ControllerBase
{
    private readonly TemperatureRepository _temperatureRepository;

    public TemperatureApiController(TemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    [HttpPost]
    [Route("graph")]
    [ProducesResponseType(typeof(GraphViewModel), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetGraph([FromBody] GraphRequest request)
    {
        var readings = await _temperatureRepository.GetCurrentReadings();
        var series = await _temperatureRepository.GetTemperatureTimeSeries(request.StartTime, request.EndTime, request.IntervalMinutes);

        return HttpResponder.Respond(new GraphViewModel
        {
            Current = readings,
            Series = series,
        });
    }
}
