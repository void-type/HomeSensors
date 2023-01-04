﻿using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Web.Controllers.Temperatures;

/// <summary>
/// Exposes temperature data through web API
/// </summary>
[ApiRoute("temperatures/readings")]
public class ReadingsApiController : ControllerBase
{
    private readonly TemperatureCachedRepository _cachedTemperatureRepository;

    public ReadingsApiController(TemperatureCachedRepository temperatureRepository)
    {
        _cachedTemperatureRepository = temperatureRepository;
    }

    [HttpPost]
    [Route("current")]
    [ProducesResponseType(typeof(List<Reading>), 200)]
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
}