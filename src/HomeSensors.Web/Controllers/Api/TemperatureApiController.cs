using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeSensors.Web.Controllers.Api;

[Route("api/temps")]
public class TemperatureApiController : ControllerBase
{
    private readonly TemperatureRepository _temperatureRepository;

    public TemperatureApiController(TemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    [HttpPost]
    public async Task<GraphViewModel> Index([FromBody] GraphRequest request)
    {
        var readings = await _temperatureRepository.GetCurrentReadings();
        var series = await _temperatureRepository.GetTemperatureTimeSeries(request.StartTime, request.EndTime, request.IntervalMinutes);

        return new GraphViewModel
        {
            Current = readings,
            Series = series,
        };
    }
}

public class GraphRequest
{
    public int IntervalMinutes { get; init; } = 15;
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now.AddHours(-48);
    public DateTimeOffset EndTime { get; init; } = DateTimeOffset.Now;
}
